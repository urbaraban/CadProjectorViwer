using MahApps.Metro.Controls;
using Microsoft.Win32;
using MonchaCadViewer.Calibration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AppSt = MonchaCadViewer.Properties.Settings;
using System.Linq;
using System.Diagnostics;
using KompasLib.Tools;
using MonchaCadViewer.CanvasObj;
using System.Globalization;
using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using KompasLib.KompasTool;
using ToGeometryConverter.Format;
using HelixToolkit.Wpf;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using MonchaSDK.ILDA;
using StclLibrary.Laser;

namespace MonchaCadViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static Dispatcher dispatcher = System.Windows.Application.Current.Dispatcher;

        private string qrpath = string.Empty;

        private KmpsAppl kmpsAppl;
        private CadCanvas MainCanvas;
        //private DotShape[,] BaseMeshRectangles;

        public MainWindow()
        {
            InitializeComponent();
            MonchaHub.RefreshedDevice += MonchaHub_RefreshDevice;

            MultPanel.NeedUpdate += MultPanel_NeedUpdate;
            DevicePanel.NeedUpdate += MultPanel_NeedUpdate;
            ObjectPanel.NeedUpdate += MultPanel_NeedUpdate;

            LoadMoncha();

            MultPanel.DataContext = MonchaHub.ProjectionSetting;

            if (DevicePanel.Device == null && MonchaHub.Devices.Count > 0)
            {
                DevicePanel.DataContext = MonchaHub.Devices[0];
            }

            CanvasBoxBorder.BorderThickness = new Thickness(MonchaHub.GetThinkess());

            this.MainCanvas = new CadCanvas(MonchaHub.Size, true);
            this.MainCanvas.Focusable = true;
            this.MainCanvas.SelectedObject += CadCanvas_SelectedObject;
            this.MainCanvas.ContextMenu = new ContextMenu();
            ContextMenuLib.CanvasMenu(this.MainCanvas.ContextMenu);
            //cadCanvas.ErrorMessageEvent += CadCanvas_ErrorMessageEvent;

            CanvasBox.Child = this.MainCanvas;
        }

        private void MultPanel_NeedUpdate(object sender, EventArgs e)
        {
            if (this.IsLoaded == true)
            {
                this.MainCanvas.UpdateProjection(false);
            }
        }

        private void CadCanvas_ErrorMessageEvent(object sender, string e)
        {
            LogBox.Items.Add(e);
        }



        private void MonchaHub_UpdatedFrame(object sender, LObjectList e)
        {
            // if (CanvasBox.Child != null)
            //   SendProcessor.Worker(CanvasBox.Child as CadCanvas);
        }

        private void CadCanvas_SelectedObject(object sender, bool e)
        {
            if (e == true)
            {
                ObjectPanel.DataContext = sender;
                MultPanel.DataContext = sender;
            }
        }

        private void MonchaHub_RefreshDevice(object sender, List<MonchaDevice> e)
        {
            DeviceTree.Items.Clear();

            TreeViewItem LaserScanners = new TreeViewItem();
            LaserScanners.Header = "LaserScanners";

            DeviceTree.Items.Add(LaserScanners);

            foreach (MonchaDevice device in MonchaHub.Devices)
            {
                if (device != null)
                {
                    TreeViewItem treeViewDevice = new TreeViewItem();
                    treeViewDevice.Header = device.HWIdentifier;
                    treeViewDevice.DataContext = device;
                    treeViewDevice.ContextMenu = new ContextMenu();
                    ContextMenuLib.DeviceTreeMenu(treeViewDevice.ContextMenu);
                    treeViewDevice.MouseLeftButtonUp += LoadDeviceSetting;
                    treeViewDevice.ContextMenuClosing += TreeViewDevice_ContextMenuClosing;

                    if (device.PolyMeshUsed)
                    {
                        TreeViewItem treeViewBaseMesh = new TreeViewItem();
                        treeViewBaseMesh.Header = "BaseMesh";
                        treeViewBaseMesh.DataContext = device.BaseMesh;
                        treeViewBaseMesh.MouseDoubleClick += TreeBaseMesh;

                        TreeViewItem treeViewVirtualMesh = new TreeViewItem();
                        treeViewVirtualMesh.Header = "VirtualMesh";
                        treeViewVirtualMesh.DataContext = device.VirtualMesh;
                        treeViewVirtualMesh.MouseDoubleClick += TreeBaseMesh;


                        if (treeViewBaseMesh.ContextMenu == null) treeViewBaseMesh.ContextMenu = new System.Windows.Controls.ContextMenu();
                        ContextMenuLib.MeshMenu(treeViewBaseMesh.ContextMenu);
                        treeViewBaseMesh.ContextMenuClosing += TreeViewBaseMesh_ContextMenuClosing;

                        if (treeViewVirtualMesh.ContextMenu == null) treeViewVirtualMesh.ContextMenu = new System.Windows.Controls.ContextMenu();
                        ContextMenuLib.MeshMenu(treeViewVirtualMesh.ContextMenu);
                        treeViewVirtualMesh.ContextMenuClosing += TreeViewBaseMesh_ContextMenuClosing;

                        treeViewDevice.Items.Add(treeViewBaseMesh);
                        treeViewDevice.Items.Add(treeViewVirtualMesh);
                    }

                    TreeViewItem treeViewCalculationMesh = new TreeViewItem();
                    treeViewCalculationMesh.Header = "CalculateMesh";
                    treeViewCalculationMesh.DataContext = device.CalculateMesh;
                    treeViewCalculationMesh.MouseDoubleClick += TreeBaseMesh;

                    if (treeViewCalculationMesh.ContextMenu == null) treeViewCalculationMesh.ContextMenu = new System.Windows.Controls.ContextMenu();
                    ContextMenuLib.MeshMenu(treeViewCalculationMesh.ContextMenu);
                    treeViewCalculationMesh.ContextMenuClosing += TreeViewBaseMesh_ContextMenuClosing;

                    treeViewDevice.Items.Add(treeViewCalculationMesh);



                    LaserScanners.Items.Add(treeViewDevice);
                    LaserScanners.ExpandSubtree();
                }
            }

            TreeViewItem LaserMeters = new TreeViewItem();
            LaserMeters.Header = "LaserMeters";
            LaserMeters.ContextMenu = new ContextMenu();
            ContextMenuLib.LaserMeterHeadTreeMenu(LaserMeters.ContextMenu);
            LaserMeters.ContextMenuClosing += LaserMeters_ContextMenuClosing;
            DeviceTree.Items.Add(LaserMeters);


            if (MonchaHub.LMeters.Count > 0)
            {
                foreach (VLTLaserMeters device in MonchaHub.LMeters)
                {
                    TreeViewItem treeLaserMeterDevice = new TreeViewItem();
                    treeLaserMeterDevice.Header = "LaserMeter " + device.IP;
                    treeLaserMeterDevice.DataContext = device;
                    treeLaserMeterDevice.ContextMenu = new ContextMenu();
                    treeLaserMeterDevice.MouseDoubleClick += TreeLaserMeterDevice_MouseDoubleClick;
                    ContextMenuLib.LaserMeterDeviceTreeMenu(treeLaserMeterDevice.ContextMenu);
                    LaserMeters.Items.Add(treeLaserMeterDevice);
                }
            }
        }

        private void TreeLaserMeterDevice_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                MonchaCadViewer.DeviceManager.LaserMeterWindows laserMeterWindows = new MonchaCadViewer.DeviceManager.LaserMeterWindows(viewItem.DataContext as VLTLaserMeters);
                laserMeterWindows.ShowDialog();
            }
        }

        private void LaserMeters_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                if (viewItem.ContextMenu.DataContext is MenuItem cmindex && sender is TreeViewItem treeView)

                    switch (cmindex.Header)
                    {
                        case "Add":
                            MonchaCadViewer.DeviceManager.LaserMeterWindows laserMeterWindows = new MonchaCadViewer.DeviceManager.LaserMeterWindows(new VLTLaserMeters());
                            laserMeterWindows.ShowDialog();
                            MonchaHub.RefreshDevice();
                            break;
                    }
            }
        }


        private void LoadMoncha()
        {
            //check path to setting file
            if (AppSt.Default.cl_moncha_path == string.Empty)
            {
                BrowseMoncha(); //select if not
            }

            //recheck
            if (File.Exists(AppSt.Default.cl_moncha_path))
            {
                //send path to hub class
                MonchaHub.Load(AppSt.Default.cl_moncha_path);

                WidthUpDn.DataContext = MonchaHub.Size;
                WidthUpDn.SetBinding(NumericUpDown.ValueProperty, "X");

                HeightUpD.DataContext = MonchaHub.Size;
                HeightUpD.SetBinding(NumericUpDown.ValueProperty, "Y");

                DeepUpDn.DataContext = MonchaHub.Size;
                DeepUpDn.SetBinding(NumericUpDown.ValueProperty, "Z");

                MashMultiplierUpDn.Value = MonchaHub.Size.M.X;
            }
        }


        private void TreeBaseMesh(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem BaseMeshItem && BaseMeshItem.Parent is TreeViewItem DeviceTree)
            {
                if (DeviceTree.DataContext is MonchaDevice device && BaseMeshItem.DataContext is MonchaDeviceMesh mesh && CanvasBox.Child is CadCanvas canvas)
                {
                    canvas.DrawMesh(mesh, device);

                    PointsVisual3D points = new PointsVisual3D();
                    points.Size = 20;

                    for (int i = 0; i < mesh.GetLength(0); i++)
                        for (int j = 0; j < mesh.GetLength(1); j++)
                        {
                            points.Points.Add(new Point3D(mesh[i, j].MX, mesh[i, j].MY, mesh[i, j].MZ));
                        }
                    HelixCanvas.Children.Clear();
                    HelixCanvas.Children.Add(points);
                }
            }
        }

        private void TreeViewDevice_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                if (viewItem.ContextMenu.DataContext is MenuItem cmindex && sender is TreeViewItem treeView)

                    switch (cmindex.Header)
                    {
                        case "%ZoneRectangle%":
                            SendProcessor.DrawZone(viewItem.DataContext as MonchaDevice);
                            break;

                        case "%CanvasRectangle%":
                            if (CanvasBox.Child is CadCanvas canvas &&
                            treeView.DataContext is MonchaDevice device)
                            {
                                canvas.DrawRectangle(device.BBOP, device.BTOP);
                            }
                            break;
                        case "%PolyMeshUsed%":
                            if (treeView.DataContext is MonchaDevice device2)
                            {
                                device2.PolyMeshUsed = !device2.PolyMeshUsed;
                                MonchaHub.RefreshDevice();
                            }
                            break;


                    }
            }
        }

        private void TreeViewBaseMesh_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                if (viewItem.ContextMenu.DataContext is MenuItem cmindex)
                {
                    if (sender is TreeViewItem meshTree && meshTree.Parent is TreeViewItem DeviceTree && DeviceTree.DataContext is MonchaDevice device && meshTree.DataContext is MonchaDeviceMesh deviceMesh)
                    {
                        switch (cmindex.Header)
                        {
                            case "Create":
                                CreateGridWindow createGridWindow = new CreateGridWindow(DeviceTree.DataContext as MonchaDevice, meshTree.DataContext as MonchaDeviceMesh);
                                createGridWindow.ShowDialog();
                                break;
                            case "Refresh":
                                deviceMesh = mws.GetMeshDot(device.HWIdentifier, deviceMesh.Name, deviceMesh.Affine);
                                break;
                            case "Inverse":
                                deviceMesh.InverseYPosition();
                                break;
                            case "ReturnPoint":
                                deviceMesh.ReturnPoint();
                                break;
                            case "Morph":
                                deviceMesh.Morph = !deviceMesh.Morph;
                                break;
                            case "Affine":
                                deviceMesh.Affine = !deviceMesh.Affine;
                                break;
                        }
                    }
                }
            }
        }

        private void LoadDeviceSetting(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem)
            {
                DevicePanel.DataContext = (MonchaDevice)treeViewItem.DataContext;
            }
        }


        private void BrowseMonchaBtn_Click(object sender, RoutedEventArgs e)
        {
            BrowseMoncha();
        }

        private void BrowseMoncha()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Moncha (.mws)|*.mws|All Files (*.*)|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                AppSt.Default.cl_moncha_path = fileDialog.FileName;
                AppSt.Default.Save();
                MonchaPathBox.Content = fileDialog.FileName;
                LoadMoncha();
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Сохранить настройки?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            switch (messageBoxResult)
            {
                case MessageBoxResult.Yes:
                    AppSt.Default.Save();
                    MonchaHub.Save();

                    break;
                case MessageBoxResult.No:
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }


        private void Window_Closed(object sender, System.EventArgs e)
        {
            MonchaHub.CanPlay = false;
            MonchaHub.Disconnect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            this.Close();
        }


        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            MonchaHub.CanPlay = !MonchaHub.CanPlay;
            if (MonchaHub.CanPlay)
            {
                if (MonchaHub.Devices.Count > 0)
                {
                    MonchaHub.Play();
                    PlayBtn.Background = Brushes.YellowGreen;
                }
                else
                {
                    MessageBox.Show("Не найден проектор. Нажмите 'Обновить'", "Внимание", MessageBoxButton.OK);
                }
            }
            else
            {
                PlayBtn.Background = Brushes.Yellow;
            }

        }


        private void SaveMeshBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DevicePanel.Device != null)
            {
                MonchaHub.Save();
            }
        }

        private void OpenBtn_Click(object sender, EventArgs e)
        {
            WinForms.OpenFileDialog openFile = new WinForms.OpenFileDialog();
            openFile.Filter = "(*.frw; *.cdw; *.svg; *.dxf; *.stp; *.ild)|*.frw; *.cdw; *.svg; *.dxf, *.stp, *.ild| All Files (*.*)|*.*";

            if (AppSt.Default.save_work_folder == string.Empty)
            {
                WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();

                if (folderDialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    AppSt.Default.save_work_folder = folderDialog.SelectedPath;
                    AppSt.Default.Save();
                }
            }

            openFile.InitialDirectory = AppSt.Default.save_work_folder;
            openFile.FileName = null;
            if (openFile.ShowDialog() == WinForms.DialogResult.OK)
                OpenFile(openFile.FileName);
        }

        private void OpenFile(string filename)
        {
            List<Shape> _actualFrames = new List<Shape>();

            if (filename.Split('.').Last() == "svg")
            {
                _actualFrames = SVG.Get(filename);
            }
            else if (filename.Split('.').Last() == "dxf")
            {
                _actualFrames = DXF.Get(filename, MonchaHub.ProjectionSetting.PointStep.MX);
            }
            else if (filename.Split('.').Last() == "ild")
            {
              //  _actualFrames = IldaReader.ReadFile(filename);
            }
            else if ((filename.Split('.').Last() == "frw") || (filename.Split('.').Last() == "cdw"))
            {
                if (KmpsAppl.KompasAPI == null)
                {
                    Process.Start(filename);
                }
                else
                {
                    this.kmpsAppl.OpenFile(filename);
                }
            }


            /*
            else if (filename.Split('.').Last() == "ild")
                MonchaWrt(filename);
            */
            if (_actualFrames == null)
            {
                return;
            }
            if (_actualFrames.Count > 0)
                AppSt.Default.stg_last_file_path = filename;
            else return;

            AppSt.Default.Save();

            ContourProcessor(false, _actualFrames);
        }

        private void ContourProcessor(bool remove, List<Shape> shapes, bool show = true)
        {
            if (remove)
            {
                FrameTree.Items.Clear();
                FrameStack.Children.Clear();
            }

            CadObjectsGroup cadObjectsGroup = new CadObjectsGroup(shapes);

            Border _viewborder = new Border();
            _viewborder.BorderThickness = new Thickness(1, 0, 1, 0);
            _viewborder.BorderBrush = Brushes.Gray;
            _viewborder.Width = FrameStack.ActualHeight;
            _viewborder.Background = Brushes.Gray;

            Viewbox _viewbox = new Viewbox();
            _viewbox.Stretch = Stretch.Uniform;
            _viewbox.StretchDirection = StretchDirection.DownOnly;
            _viewbox.Margin = new Thickness(0);
            _viewbox.DataContext = cadObjectsGroup;
            _viewbox.ClipToBounds = true;
            _viewbox.Cursor = Cursors.Hand;
            _viewbox.MouseLeftButtonUp += DrawTreeContour;
            _viewbox.MouseRightButtonUp += _viewbox_MouseRightButtonUp;


            Viewbox _canvasviewbox = new Viewbox();
            _canvasviewbox.Stretch = Stretch.Uniform;
            _canvasviewbox.StretchDirection = StretchDirection.DownOnly;
            _canvasviewbox.Margin = new Thickness(0);

            CadCanvas _canvas = new CadCanvas(MonchaHub.Size, false);
            _canvas.Background = Brushes.White;

            _canvasviewbox.Child = _canvas;
            _viewbox.Child = _canvasviewbox;
            _viewborder.Child = _viewbox;
            FrameStack.Children.Add(_viewborder);


            _canvas.DrawContour(new CadContour(cadObjectsGroup, false, false), false, false, true);

            TreeViewItem contourTree = new TreeViewItem();
            //contourTree.Header = contoursList.DisplayName == string.Empty ? "Frame " + FrameTree.Items.Count : "Frame:" + contoursList.DisplayName;
            contourTree.DataContext = shapes;
            contourTree.MouseLeftButtonUp += DrawTreeContour;

            FrameTree.Items.Add(contourTree);

            if (show)
            {
                this.MainCanvas.Clear();

                foreach (CadObject cadObject in cadObjectsGroup.Objects)
                {
                    this.MainCanvas.DrawContour(cadObject, true, true, true);
                }
                this.MainCanvas.UpdateProjection(true);
            }

            LinesVisual3D linesVisual3D = new LinesVisual3D();
            linesVisual3D.Color = Color.FromRgb(255, 0, 0);
            linesVisual3D.Thickness = 1;
            linesVisual3D.Points.Add(new Point3D(0, 0, 0));
            linesVisual3D.Points.Add(new Point3D(200, 2000, 2));
            linesVisual3D.Points.Add(new Point3D(700, 0, 200));
            linesVisual3D.Points.Add(new Point3D(400, 200, 200));

            HelixCanvas.Children.Add(linesVisual3D);


        }

        private void _viewbox_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Viewbox viewItem)
            {
                if (viewItem.DataContext is CadObjectsGroup CadObj)
                {
                    CadObj.UpdateTransform();
                }
            }
        }

        private void DrawTreeContour(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Shift)
            {
                this.MainCanvas.Clear();
            }

            if (sender is TreeViewItem viewItem)
            {
                //_selectedindex = FrameTree.Items.IndexOf(viewItem);
                if (viewItem.DataContext is CadObjectsGroup cadObjectsGroup && CanvasBox.Child is CadCanvas canvas)
                {
                    foreach (CadObject cadObject in cadObjectsGroup.Objects)
                    {
                        canvas.DrawContour(cadObject, true, true, true);
                    }
                }
            }

            if (sender is Viewbox viewbox)
            {
                //_selectedindex = FrameStack.Children.IndexOf(viewbox.Parent as Border);
                if (viewbox.DataContext is CadObjectsGroup cadObjectsGroup && CanvasBox.Child is CadCanvas canvas)
                {
                    foreach (CadObject cadObject in cadObjectsGroup.Objects)
                    {
                        canvas.DrawContour(cadObject, true, true, true);
                    }
                }
                    
            }
        }


        private void OpenBtn_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                foreach (string fileLoc in filePaths)
                    if (File.Exists(fileLoc))
                        OpenFile(fileLoc);
            }
            OpenBtn.Background = Brushes.Gainsboro;
        }

        private void OpenBtn_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
            OpenBtn.Background = Brushes.Yellow;
            string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
            foreach (string fileLoc in filePaths) //переберает всю инфу пока не найдет строку адреса
                if (File.Exists(fileLoc))
                    OpenBtn.Content = "Открыть " + fileLoc.Split('\\').Last();
        }

        private void OpenBtn_DragLeave(object sender, EventArgs e)
        {
            OpenBtn.Content = "Открыть";
            OpenBtn.Background = Brushes.Gainsboro;
        }

        private void OpenBtn_DragOver(object sender, DragEventArgs e)
        {
            OpenBtn.Background = Brushes.Gainsboro;
        }

        private void ReloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AppSt.Default.stg_last_file_path != string.Empty)
                if (File.Exists(AppSt.Default.stg_last_file_path))
                    OpenFile(AppSt.Default.stg_last_file_path);
        }

        private void MashMultiplierUpDn_ValueIncremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            args.Interval = 0;
            MashMultiplierUpDn.Value = MashMultiplierUpDn.Value.Value * 10;
            MonchaHub.Size.M.Set(MashMultiplierUpDn.Value.Value);
        }

        private void MashMultiplierUpDn_ValueDecremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            args.Interval = 0;
            MashMultiplierUpDn.Value = MashMultiplierUpDn.Value.Value / 10;
            MonchaHub.Size.M.Set(MashMultiplierUpDn.Value.Value);
        }


        private void RefreshLaser_Click_1(object sender, RoutedEventArgs e)
        {
            LoadMoncha();
            treeView.UpdateLayout();
        }

        private void LineBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CanvasBox.Child is CadCanvas canvas)
            {
                if (canvas.Status != 1)
                    canvas.Status = 1;
                else
                    canvas.Status = 0;
                if (canvas.Status == 1)
                    (sender as Button).Background = Brushes.Green;
                else
                    (sender as Button).Background = Brushes.White;
            }
        }

        private void HorizontalToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
                if (CanvasBox.Child is CadCanvas cadCanvas)
                    cadCanvas.HorizontalMesh = HorizontalToggle.IsOn;
        }



        private void kmpsConnectToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI == null)
            {
                kmpsAppl = new KmpsAppl();

                if (kmpsAppl.Connect())
                {
                    kmpsAppl.ConnectBoolEvent += KmpsAppl_ConnectBoolEvent;

                    kmpsAppl.OpenedDoc += KmpsAppl_OpenedDoc;

                    kmpsConnectToggle.IsOn = KmpsAppl.KompasAPI != null;

                    kmpsAppl.SelectDoc();

                }

            }
        }

        private void KmpsAppl_OpenedDoc(object sender, object e)
        {
            if (e is KmpsDoc kmpsDoc)
                KmpsNameLbl.Content = kmpsDoc.D7.Name;
        }

        private void KmpsAppl_ChangeDoc(object sender, KmpsDoc e)
        {
            KmpsNameLbl.Invoke(new Action(() => {
                if (e.D7 != null)
                    KmpsNameLbl.Content = e.D7.Name;
            }));
        }

        private void KmpsAppl_ConnectBoolEvent(object sender, bool e)
        {
            kmpsConnectToggle.IsOn = e;
        }

        private void stlSeparatorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (svgSeparatorBox1.Text != string.Empty)
                AppSt.Default.svg_separator1 = svgSeparatorBox1.Text.Last();
            if (svgSeparatorBox2.Text != string.Empty)
                AppSt.Default.svg_separator2 = svgSeparatorBox2.Text.Last();
        }

        private void kmpsSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                ContourProcessor(false, ContourCalc.GetGeometry(this.kmpsAppl.Doc, MonchaHub.ProjectionSetting.PointStep.MX, false, true));
            }

        }

        private void kmpsAddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                ContourProcessor(false, ContourCalc.GetGeometry(this.kmpsAppl.Doc, MonchaHub.ProjectionSetting.PointStep.MX, true, true));
            }
        }




        private void MainWindow1_KeyUp(object sender, KeyEventArgs e)
        {
            double _mult = Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 1;
            double step = PointStepUpDn.Value.Value;

            switch (e.Key)
            {
                case Key.Q:
                    SelectNext();
                    break;
                case Key.W:
                    MoveCanvasSet(0, -step * _mult);
                    break;
                case Key.S:
                    MoveCanvasSet(0, step * _mult);
                    break;
                case Key.A:
                    MoveCanvasSet(-step * _mult, 0);
                    break;
                case Key.D:
                    MoveCanvasSet(step * _mult, 0);
                    break;
                case Key.F:
                    MirrorPosition();
                    break;
                case Key.OemPlus:
                    MirrorPosition();
                    break;
                case Key.Delete:
                    this.MainCanvas.RemoveSelectObject();
                    break;
                case Key.D1:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DeviceBright(2);
                    }
                    break;
                case Key.D2:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DeviceBright(3);
                    }
                    break;
                case Key.D3:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DeviceBright(4);
                    }
                    break;
                case Key.D4:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DeviceBright(5);
                    }
                    break;
                case Key.D5:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DeviceBright(6);
                    }
                    break;
                case Key.D6:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DeviceBright(7);
                    }
                    break;
                case Key.D7:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DeviceBright(8);
                    }
                    break;
                case Key.D8:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DeviceBright(9);
                    }
                    break;
                case Key.D9:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DeviceBright(10);
                    }
                    break;

            }

            void DeviceBright(double TenPercent)
            {
                DevicePanel.Device.Alpha = (byte)(255 * TenPercent / 10);
            }

            void SelectNext()
            {
                if (CanvasBox.Child is CadCanvas canvas)
                {
                    for (int i = 0; i < canvas.Children.Count; i++)
                    {
                        if (canvas.Children[i] is CadObject cadObject)
                        {
                            if (cadObject.IsSelected)
                            {
                                cadObject.IsFix = true;
                                cadObject.IsSelected = false;

                                try
                                {
                                    if (canvas.Children[i + (InverseToggle.IsOn ? -1 : +1)] is CadObject cadObject2)
                                    {
                                        cadObject2.IsSelected = true;
                                        cadObject2.IsFix = false;
                                    }
                                }
                                catch (Exception exeption)
                                {
                                    LogBox.Items.Add($"Main: {exeption.Message}");
                                    InverseToggle.IsOn = !InverseToggle.IsOn;
                                    cadObject.IsSelected = true;
                                }
                                break;
                            }
                        }
                    }
                }
            }

            void MoveCanvasSet(double left, double top)
            {
                for (int i = 0; i < this.MainCanvas.Children.Count; i++)
                {
                    if (this.MainCanvas.Children[i] is CadObject cadObject)
                    {
                        if (cadObject.IsSelected && !cadObject.IsFix)
                        {
                            cadObject.X += left;
                            cadObject.Y += top;
                        }
                    }
                }

                
            }

            void MirrorPosition()
            {
                if (CanvasBox.Child is CadCanvas canvas)
                {
                    for (int i = 0; i < canvas.Children.Count; i++)
                    {
                        if (canvas.Children[i] is CadObject cadObject1)
                        {
                            if (cadObject1.IsSelected)
                            {
                                if (cadObject1.DataContext is MonchaDeviceMesh deviceMesh && cadObject1 is CadDot dot)
                                {
                                    deviceMesh.MirrorPoint(dot.Point);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void MainWindow1_TextInput(object sender, TextCompositionEventArgs e)
        {
            string keyChar = e.Text;
            if (QRBtn.IsChecked.Value)
            {
                qrpath += keyChar;
                LogBox.Items.Add((AppSt.Default.save_qr_path ? AppSt.Default.save_base_folder : AppSt.Default.save_work_folder) + @qrpath);
                if (File.Exists((AppSt.Default.save_qr_path ? AppSt.Default.save_base_folder : AppSt.Default.save_work_folder) + @qrpath))
                {
                    //Progressinfo.Content = "Файл найден!";
                    OpenFile((AppSt.Default.save_qr_path ? AppSt.Default.save_base_folder : AppSt.Default.save_work_folder) + @qrpath);
                }
                else
                {
                    // Progressinfo.Content = "Файл не найден!";
                }
            }
        }

        private void MainWindow1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (QRBtn.IsChecked.Value)
                InputLanguageManager.SetInputLanguage(this, new CultureInfo("ru-RU"));

            int Temp = KeyInterop.VirtualKeyFromKey(e.Key);
            //Debug.WriteLine(Temp);
            // KeyInterop.KeyFromVirtualKey(e.Key);
            if (qrpath.Contains("#"))
            {
                qrpath = string.Empty;
                AppSt.Default.save_qr_path = true;

            }
            else if (qrpath.Contains("&"))
            {
                qrpath = string.Empty;
                AppSt.Default.save_qr_path = false;

            }
            QrToggler.IsOn = AppSt.Default.save_qr_path;
        }

        private void QRBtn_Click(object sender, RoutedEventArgs e)
        {
            if (QRBtn.IsChecked.Value)
            {

            }

        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CanvasBox.Child is CadCanvas canvas)
            {
                canvas.Clear();
            }
        }

        private void QrToggler_Toggled(object sender, RoutedEventArgs e)
        {
            AppSt.Default.save_qr_path = QrToggler.IsOn;
            AppSt.Default.Save();
        }

        private void BaseFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog WorkFolderSlct = new System.Windows.Forms.FolderBrowserDialog();

            if (WorkFolderSlct.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (WorkFolderSlct.SelectedPath != string.Empty)
                {
                    AppSt.Default.save_base_folder = WorkFolderSlct.SelectedPath;
                    AppSt.Default.Save();
                }
            }

            BasePathBox.Text = AppSt.Default.save_base_folder;
        }

        private void WorkFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            DateTime date = DateTime.Now;

            System.Windows.Forms.FolderBrowserDialog WorkFolderSlct = new System.Windows.Forms.FolderBrowserDialog();

            if (WorkFolderSlct.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (WorkFolderSlct.SelectedPath != string.Empty)
                {
                    AppSt.Default.save_work_folder = WorkFolderSlct.SelectedPath;
                    AppSt.Default.lastDate = date.Month;
                    AppSt.Default.Save();
                }
            }

            AlreadyPathBox.Text = AppSt.Default.save_work_folder;
        }

        private void MainViewBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
            OpenBtn.Background = Brushes.Yellow;
            string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
            if (filePaths != null)
                foreach (string fileLoc in filePaths) //переберает всю инфу пока не найдет строку адреса
                    if (File.Exists(fileLoc))
                        OpenBtn.Content = "Открыть " + fileLoc.Split('\\').Last();
            CanvasBorder.Background = Brushes.LightYellow;
        }

        private void MainViewBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                foreach (string fileLoc in filePaths)
                    if (File.Exists(fileLoc))
                        OpenFile(fileLoc);
            }
            OpenBtn.Background = Brushes.Gainsboro;
            CanvasBorder.Background = Brushes.LightGray;
        }

        private void AddLaser_Click(object sender, RoutedEventArgs e)
        {
            LaserManager deviceManager = new LaserManager();
            deviceManager.Show();
        }


        private void MinimizedBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void FullSizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else this.WindowState = WindowState.Normal;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            AppSt.Default.Save();
            MonchaHub.Save();
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Helix_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void PointStepUpDn_ValueDecremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            if ((Math.Round(PointStepUpDn.Value.Value, 4) - PointStepUpDn.Interval) == 0)
            {
                PointStepUpDn.Interval = PointStepUpDn.Interval / 10;
                args.Interval = args.Interval / 10;
            }
        }

        private void PointStepUpDn_ValueIncremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            if (Math.Round(PointStepUpDn.Value.Value + PointStepUpDn.Interval, 4) >= PointStepUpDn.Interval * 10)
            {
                PointStepUpDn.Interval = PointStepUpDn.Interval * 10;
            }
        }

        private void ILDASaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "(*.ild)|*.ild| All Files (*.*)|*.*";
            saveFileDialog.ShowDialog();
            IldaWriter ildaWriter = new IldaWriter();

            if (saveFileDialog.FileName != string.Empty)
            {
                for (int i = 0; i < MonchaHub.Devices.Count; i++)
                {
                    ildaWriter.Write(($"{saveFileDialog.FileName.Replace(".ild", string.Empty)}_{i}.ild"), new List<LFrame>()
                {
                    MonchaHub.Devices[i].GetReadyFrame.GetLFrame(MonchaHub.Devices[i], MonchaHub.MainFrame)
                }, 5);
                }
            }
        }
    }

}
