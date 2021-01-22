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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.Windows.Data;

namespace MonchaCadViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Dispatcher dispatcher = System.Windows.Application.Current.Dispatcher;

        private string qrpath = string.Empty;

        private KmpsAppl kmpsAppl;
        private bool inverseToggle = true;
        //private DotShape[,] BaseMeshRectangles;

        public MainWindow()
        {
            InitializeComponent();
            MonchaHub.Loging += MonchaHub_Loging;
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

            this.MainCanvas.Focusable = true;

            this.MainCanvas.SelectedObject += MainCanvas_SelectedObject;

            ContourScrollPanel.SelectedFrame += ContourScrollPanel_SelectedFrame;
        }

        private void MainCanvas_SelectedObject(object sender, CadObject e)
        {
            MultPanel.DataContext = e;
            ObjectPanel.DataContext = e;
        }

        private void ContourScrollPanel_SelectedFrame(object sender, CadObjectsGroup e)
        {
            if (e != null)
            {
                this.MainCanvas.DataContext = null;
                this.MainCanvas.DataContext = e;
            }
        }

        private void MonchaHub_Loging(object sender, string e) => LogBox.Invoke(() => { LogBox.Items.Add(e); });


        private void MultPanel_NeedUpdate(object sender, EventArgs e)
        {
            if (this.IsLoaded == true)
            {
                this.MainCanvas.UpdateProjection(false);
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
            if (File.Exists(AppSt.Default.cl_moncha_path) == false)
            {
                BrowseMoncha(); //select if not
            }
            MonchaHub.Disconnect();

            //send path to hub class
            MonchaHub.Load(AppSt.Default.cl_moncha_path);

            WidthUpDn.DataContext = MonchaHub.Size;
            WidthUpDn.SetBinding(NumericUpDown.ValueProperty, "X");

            HeightUpD.DataContext = MonchaHub.Size;
            HeightUpD.SetBinding(NumericUpDown.ValueProperty, "Y");

            MashMultiplierUpDn.Value = MonchaHub.Size.M.X;

            CalibrationFormCombo.Items.Clear();
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_Dot);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_Rect);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_Cross);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_HLine);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_WLine);

        }


        private void TreeBaseMesh(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem BaseMeshItem && BaseMeshItem.Parent is TreeViewItem DeviceTree)
            {
                if (DeviceTree.DataContext is MonchaDevice device && BaseMeshItem.DataContext is MonchaDeviceMesh mesh)
                {
                    MainCanvas.DrawMesh(mesh, device);
                }
            }
        }

        private void TreeViewDevice_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                if (viewItem.ContextMenu.DataContext is MenuItem cmindex && sender is TreeViewItem treeView &&
                            treeView.DataContext is MonchaDevice device)

                    switch (cmindex.Header)
                    {
                        case "%ZoneRectangle%":
                            device.DrawZone();
                            break;

                        case "%CanvasRectangle%":

                                MainCanvas.DrawRectangle(device.BBOP, device.BTOP);
                            
                            break;
                        case "%PolyMeshUsed%":
                            device.PolyMeshUsed = !device.PolyMeshUsed;
                            MonchaHub.RefreshDevice();
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
                                deviceMesh = MonchaHub.MWS.GetMeshDot(device.HWIdentifier, deviceMesh.Name, deviceMesh.Affine);
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
            e.Cancel = SaveConfiguration(false);
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            MonchaHub.CanPlay = false;
            MonchaHub.Disconnect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
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
                _actualFrames = SVG.Get(filename, AppSt.Default.defailt_tesselate);
            }
            else if (filename.Split('.').Last() == "dxf")
            {
                _actualFrames = DXF.Get(filename, MonchaHub.ProjectionSetting.PointStep.MX);
            }
            else if (filename.Split('.').Last() == "ild")
            {
              //  _actualFrames = IldaReader.ReadFile(filename);
            }
            else if (filename.Split('.').Last() == "dc")
            {
                _actualFrames = DC.Get(filename);
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

            ContourScrollPanel.Add(false, _actualFrames, filename.Split('\\').Last());
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
            if (MashMultiplierUpDn.Value == null) MashMultiplierUpDn.Value = 1;
            args.Interval = 0;
            MashMultiplierUpDn.Value = MashMultiplierUpDn.Value.Value * 10;
            MonchaHub.Size.M.Set(MashMultiplierUpDn.Value.Value, true);
        }

        private void MashMultiplierUpDn_ValueDecremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            args.Interval = 0;
            MashMultiplierUpDn.Value = MashMultiplierUpDn.Value.Value / 10;
            MonchaHub.Size.M.Set(MashMultiplierUpDn.Value.Value, true);
        }


        private void RefreshLaser_Click_1(object sender, RoutedEventArgs e)
        {
            MonchaHub.CanPlay = false;
            LoadMoncha();
            treeView.UpdateLayout();
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
            KmpsNameLbl.Invoke(() => { 
            if (e is KmpsDoc kmpsDoc)
            {
                if (kmpsDoc.D7.Name != null)
                {
                    KmpsNameLbl.Content = kmpsDoc.D7.Name;
                }
                else
                {
                    KmpsNameLbl.Content = "Пустой";
                }
            }
            });
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


        private void kmpsSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                ContourScrollPanel.Add(false, ContourCalc.GetGeometry(this.kmpsAppl.Doc, MonchaHub.ProjectionSetting.PointStep.MX, false, true), this.kmpsAppl.Doc.D7.Name);
            }

        }

        private void kmpsAddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                ContourScrollPanel.Add(false, ContourCalc.GetGeometry(this.kmpsAppl.Doc, MonchaHub.ProjectionSetting.PointStep.MX, true, true), this.kmpsAppl.Doc.D7.Name);
            }
        }




        private void MainWindow1_KeyUp(object sender, KeyEventArgs e)
        {
            double _mult = Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 1;
            double step = PointStepUpDn.Value.Value;

            switch (e.Key)
            {
                case Key.Q:
                    MainCanvas.SelectNext();
                    break;
                case Key.W:
                    MainCanvas.MoveCanvasSet(0, -step * _mult);
                    break;
                case Key.S:
                    MainCanvas.MoveCanvasSet(0, step * _mult);
                    break;
                case Key.A:
                    MainCanvas.MoveCanvasSet(-step * _mult, 0);
                    break;
                case Key.D:
                    MainCanvas.MoveCanvasSet(step * _mult, 0);
                    break;
                case Key.F:
                    MainCanvas.FixPosition();
                    break;
                case Key.OemPlus:

                    break;
                case Key.Delete:
                   // this.MainCanvas.RemoveSelectObject();
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
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            MainCanvas.Clear();
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

        private void MenuSaveBtn_Click(object sender, RoutedEventArgs e) => SaveConfiguration(false);  

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
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

        /// <summary>
        /// Save frame to ILDA file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ILDASaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "(*.ild)|*.ild| All Files (*.*)|*.*";
            saveFileDialog.ShowDialog();
            IldaWriter ildaWriter = new IldaWriter();

            if (saveFileDialog.FileName != string.Empty)
            {
                for (int i = 0; i < MonchaHub.Devices.Count; i++)
                {
                    ildaWriter.Write(($"{saveFileDialog.FileName.Replace(".ild", string.Empty)}_{i}.ild"), new List<LFrame>(){await MonchaHub.Devices[i].GetReadyFrame.GetLFrame(MonchaHub.Devices[i], MonchaHub.MainFrame, UsedMeshToggle.IsOn)}, 5);
                }
            }
        }

        private void RemoveLaser_Click(object sender, RoutedEventArgs e) => MonchaHub.RemoveDevice(DevicePanel.Device);

        private void SaveObjStgBtn_Click(object sender, RoutedEventArgs e)
        {
            AppSt.Default.default_scale_x = ScaleXBox.Value.Value;
            AppSt.Default.default_scale_y = ScaleYBox.Value.Value;
            AppSt.Default.default_angle = AngleBox.Value.Value;
            AppSt.Default.default_mirror = MirrorBox.IsChecked.Value;
            AppSt.Default.defailt_tesselate = TesselateCheck.IsChecked.Value;
            AppSt.Default.stg_scale_invert = ScaleInvertCheck.IsChecked.Value;
            AppSt.Default.stg_scale_percent = ScalePercentCheck.IsChecked.Value;
            AppSt.Default.Save();
        }

        private void CalibrationFormCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CalibrationFormCombo.SelectedValue != null)
            {
                MonchaDeviceMesh.ClbrForm = (CalibrationForm)CalibrationFormCombo.SelectedValue;
                this.MainCanvas.UpdateProjection(true);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            MonchaHub.Save(AppSt.Default.cl_moncha_path);
            AppSt.Default.Save();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
                ReadyFrame.SetEndgePoint = checkBox.IsChecked.Value;
            MonchaHub.RefreshFrame();
        }

        private void WorkFolderRefreshBtn_Click(object sender, RoutedEventArgs e) => RefreshWorkFolderList();

        private void RefreshWorkFolderList()
        {
            if(Directory.Exists(AppSt.Default.save_work_folder) == true)
            {
                List<string> paths = new List<string>();
                foreach (string path in Directory.GetFiles(AppSt.Default.save_work_folder))
                {
                    string format = path.Split('.').Last();
                    if (format == "svg" || format == "dxf" || format == "frw" || format == "dc") {
                        paths.Add(path.Split('\\').Last());
                    }
                }
                WorkFolderListBox.ItemsSource = paths;
                CollectionView view = CollectionViewSource.GetDefaultView(WorkFolderListBox.ItemsSource) as CollectionView;
                WorkFolderListBox.Items.Filter = new Predicate<object>(Contains);
            }
        }

        public bool Contains(object pt)
        {
            string path = pt as string;
            //Return members whose Orders have not been filled
            return path.ToLower().Contains(WorkFolderFilter.Text.ToLower());
        }

        private void WorkFolderListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WorkFolderListBox.SelectedItem != null)
            {
                OpenFile($"{AppSt.Default.save_work_folder}\\{WorkFolderListBox.SelectedItem.ToString()}");
            }
        }

        private void WorkFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                if (Directory.Exists(dialog.FileName) == true)
                {
                    AppSt.Default.save_work_folder = dialog.FileName;
                    AppSt.Default.Save();
                    RefreshWorkFolderList();
                }
            }
           
        }

        private void WorkFolderFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WorkFolderListBox.Items.Count < 1)
            {
                RefreshWorkFolderList();
            }
            CollectionViewSource.GetDefaultView(WorkFolderListBox.ItemsSource).Refresh();
        }

        private void WorkFolderFilter_GotFocus(object sender, RoutedEventArgs e) => WorkFolderFilter.SelectAll();
  
        private void MainCanvas_Drop(object sender, DragEventArgs e)
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

        private void SaveAsItem_Click(object sender, RoutedEventArgs e) => SaveConfiguration(true);

        private bool SaveConfiguration(bool saveas)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Сохранить настройки?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            switch (messageBoxResult)
            {
                case MessageBoxResult.Yes:
                    if (File.Exists(AppSt.Default.cl_moncha_path) == false || saveas)
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.Filter = "Moncha File (*.mws)|*.mws";
                        if (saveFileDialog.ShowDialog() == true)
                        {
                            pBarStart("Save Moncha", 1, 2);
                            MonchaHub.Save(saveFileDialog.FileName);
                            if (File.Exists(saveFileDialog.FileName) == false)
                            {
                                pBarUpdate("Not Save", 2);
                                SaveConfiguration(true);
                            }
                            else
                            {
                                pBarUpdate("Saved", 2);
                                AppSt.Default.cl_moncha_path = saveFileDialog.FileName;
                            }                          
                        }
                    }
                    else
                    {
                        MonchaHub.Save(AppSt.Default.cl_moncha_path);
                    }
                    MonchaPathBox.Content = AppSt.Default.cl_moncha_path;
                    AppSt.Default.Save();
                    pBarEnd();
                    return false;
                    break;
                case MessageBoxResult.No:
                    pBarEnd();
                    return false;
                    break;
                case MessageBoxResult.Cancel:
                    pBarEnd();
                    return true;
                    break;

            }
            pBarUpdate("Save Setting", 2);
            
            
            pBarEnd();
            return false;
        }

        private void pBarStart(string LabelText, int startValue, int endValue)
        {
            pBarLabel.Visibility = Visibility.Visible;
            pBarLabel.Content = LabelText;
            pBar.Value = startValue;
            pBar.Maximum = endValue;
        }
        /// <summary>
        /// Update value progressbar
        /// </summary>
        /// <param name="LabelText">Status text</param>
        /// <param name="value">now value</param>
        private void pBarUpdate(string LabelText, int value)
        {
            if (pBarLabel.Visibility == Visibility.Hidden) pBarLabel.Visibility = Visibility.Visible;
            pBarLabel.Content = LabelText;
            pBar.Value = value;
        }

        private void pBarEnd()
        {
            //pBarLabel.Visibility = Visibility.Hidden;
            pBar.Value = 0;
        }
    }

}
