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
using System.Windows.Media.Media3D;
using StclLibrary.Laser;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.Windows.Data;
using MonchaCadViewer.Panels;
using ToGeometryConverter;
using ToGeometryConverter.Object;
using System.Threading.Tasks;
using ToGeometryConverter.Object.Elements;
using ToGeometryConverter.Format.ILDA;
using System.Text;
using System.Net.Sockets;
using System.Net;
using ToGeometryConverter.Object.UDP;
using MonchaCadViewer.Panels.CanvasPanel;
using System.Management;
using ToGeometryConverter.Format;
using MonchaCadViewer.StaticTools;

namespace MonchaCadViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ProjectionScene MainScene { get; } = new ProjectionScene();
        public LaserHub LaserHub { get; set; } = new LaserHub(AppSt.Default.cl_moncha_path);

        public LSize3D CanvasSize => LaserHub.Size;

        private KmpsAppl kmpsAppl;
        private bool inverseToggle = true;
        private UdpLaserListener UDPLaserListener
        {
            get => this._udpLaserListener;
            set
            {
                if (this._udpLaserListener != null)
                {
                    this._udpLaserListener.IncomingData -= UdpLaserListener_IncomingData;
                    this._udpLaserListener.Stop();
                }
                this._udpLaserListener = value;
                this._udpLaserListener.IncomingData += UdpLaserListener_IncomingData;
            }
        }
        private UdpLaserListener _udpLaserListener;

        public MainWindow()
        {
            InitializeComponent();

            #region Language
            App.LanguageChanged += LanguageChanged;

            CultureInfo currLang = App.Language;
            //Заполняем меню смены языка:
            menuLanguage.Items.Clear();
            foreach (var lang in App.Languages)
            {
                MenuItem menuLang = new MenuItem();
                menuLang.Header = lang.DisplayName;
                menuLang.Tag = lang;
                menuLang.IsChecked = lang.Equals(currLang);
                menuLang.Click += ChangeLanguageClick;
                menuLanguage.Items.Add(menuLang);
            }

            App.Language = AppSt.Default.DefaultLanguage;
            #endregion

            ProgressPanel.Label = "Hello world!";
            ToGCLogger.Progressed += ToGC_Progressed;

            LaserHub.Loging += MonchaHub_Loging;
            MainScene.UpdateFrame += MainScene_UpdateFrame;

            LoadMoncha();
        }

        private async void MainScene_UpdateFrame(object sender, EventArgs e)
        {
            LaserHub.MainFrame = await LaserSender.GetLObject(MainScene);
        }


        private void ToGC_Progressed(object sender, ProgBarMessage e)
        {
            Dispatcher.Invoke(() => ProgressPanel.SetProgressBar(e.v, e.m, e.t));
        }

        private void LanguageChanged(Object sender, EventArgs e)
        {
            CultureInfo currLang = App.Language;

            //Отмечаем нужный пункт смены языка как выбранный язык
            foreach (MenuItem i in menuLanguage.Items)
            {
                CultureInfo ci = i.Tag as CultureInfo;
                i.IsChecked = ci != null && ci.Equals(currLang);
            }
        }

        private void ChangeLanguageClick(Object sender, EventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                CultureInfo lang = mi.Tag as CultureInfo;
                if (lang != null)
                {
                    App.Language = lang;
                }
            }

        }

        private void MonchaHub_Loging(object sender, string e) => LogBox.Invoke(() => { LogBox.Items.Add(e); });


        private void LoadMoncha()
        {
            LaserHub.Play = false;

            //check path to setting file
            if (File.Exists(AppSt.Default.cl_moncha_path) == false)
            {
                BrowseMoncha(); //select if not
            }
            LaserHub.Disconnect();

            //send path to hub class
            LaserHub.Load(AppSt.Default.cl_moncha_path);

            WidthUpDn.DataContext = LaserHub.Size;
            WidthUpDn.SetBinding(NumericUpDown.ValueProperty, "X");

            HeightUpD.DataContext = LaserHub.Size;
            HeightUpD.SetBinding(NumericUpDown.ValueProperty, "Y");

            DeepUpDn.DataContext = LaserHub.Size;
            DeepUpDn.SetBinding(NumericUpDown.ValueProperty, "Z");

            MashMultiplierUpDn.Value = LaserHub.Size.M.X;

            CalibrationFormCombo.Items.Clear();
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_Dot);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_Rect);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_miniRect);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_Cross);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_HLine);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_WLine);

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
            LaserHub.Play = false;
            LaserHub.Disconnect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            LaserHub.Play = !LaserHub.Play;
        }


        private async void OpenBtn_ClickAsync(object sender, EventArgs e)
        {
            WinForms.OpenFileDialog openFile = new WinForms.OpenFileDialog();
            openFile.Filter = ToGC.Filter;
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
            {
                await OpenFile(openFile.FileName);
            }
        }

        public async Task OpenFile(string filename)
        {
            if ((filename.Split('.').Last() == "frw") || (filename.Split('.').Last() == "cdw"))
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
            else
            {

                GCCollection LoadedFrame = await GetGC.Load(filename);
                GC.Collect();
                if (LoadedFrame == null)
                {
                    return;
                }


                ContourScrollPanel.Add(false, new CadObjectsGroup(LoadedFrame), filename);
            }
        }
       

        private async void OpenBtn_DragDropAsync(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                foreach (string fileLoc in filePaths)
                    if (File.Exists(fileLoc))
                        await OpenFile(fileLoc);
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

       
        private void MashMultiplierUpDn_ValueIncremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            if (MashMultiplierUpDn.Value == null) MashMultiplierUpDn.Value = 1;
            args.Interval = 0;
            MashMultiplierUpDn.Value = MashMultiplierUpDn.Value.Value * 10;
            LaserHub.Size.M.Set(MashMultiplierUpDn.Value.Value);
        }

        private void MashMultiplierUpDn_ValueDecremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            args.Interval = 0;
            MashMultiplierUpDn.Value = MashMultiplierUpDn.Value.Value / 10;
            LaserHub.Size.M.Set(MashMultiplierUpDn.Value.Value);
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


        private async void kmpsSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                GCCollection gCElements = new GCCollection(this.kmpsAppl.Doc.D7.Name);
                gCElements.AddRange(
                    await ContourCalc.GetGeometry(this.kmpsAppl.Doc, LaserHub.ProjectionSetting.PointStep.MX, false, true));

                CadObjectsGroup cadGeometries = new CadObjectsGroup(gCElements);

                ContourScrollPanel.Add(false, cadGeometries, this.kmpsAppl.Doc.D7.Name);
            }
        }

        private async void kmpsAddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                GCCollection gCObjects = new GCCollection(this.kmpsAppl.Doc.D7.Name);

                gCObjects.Add(new GeometryElement(await ContourCalc.GetGeometry(this.kmpsAppl.Doc, LaserHub.ProjectionSetting.PointStep.MX, true, true), "Kompas"));

                CadObjectsGroup cadGeometries = new CadObjectsGroup(gCObjects);

                ContourScrollPanel.Add(true, cadGeometries, this.kmpsAppl.Doc.D7.Path);
                   
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
                        DevicePanel.DeviceBright(2);
                    }
                    break;
                case Key.D2:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DevicePanel.DeviceBright(3);
                    }
                    break;
                case Key.D3:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DevicePanel.DeviceBright(4);
                    }
                    break;
                case Key.D4:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DevicePanel.DeviceBright(5);
                    }
                    break;
                case Key.D5:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DevicePanel.DeviceBright(6);
                    }
                    break;
                case Key.D6:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DevicePanel.DeviceBright(7);
                    }
                    break;
                case Key.D7:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DevicePanel.DeviceBright(8);
                    }
                    break;
                case Key.D8:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DevicePanel.DeviceBright(9);
                    }
                    break;
                case Key.D9:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        DevicePanel.DeviceBright(10);
                    }
                    break;
                case Key.Escape:
                    MainCanvas.MouseAction = Panels.CanvasPanel.MouseAction.NoAction;
                    break;
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            MainScene.Clear();
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

        private async void MainViewBox_DropAsync(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                foreach (string fileLoc in filePaths)
                    if (File.Exists(fileLoc))
                        await OpenFile(fileLoc);
            }
            OpenBtn.Background = Brushes.Gainsboro;
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

            foreach(ScrollPanelItem item in ContourScrollPanel.FrameStack.Children)
            {
                if (item.IsSelected == true)
                {
                    saveFileDialog.FileName += $"{(saveFileDialog.FileName != string.Empty ? " " : string.Empty)}{item.Scene.NameID}";
                }
            }

            saveFileDialog.ShowDialog();
            IldaWriter ildaWriter = new IldaWriter();

            if (saveFileDialog.FileName != string.Empty)
            {
                for (int i = 0; i < LaserHub.Devices.Count; i++)
                {
                    ildaWriter.Write(($"{saveFileDialog.FileName.Replace(".ild", string.Empty)}_{i}.ild"), new List<LFrame>(){ await LaserHub.Devices[i].GetReadyFrame.GetLFrame(LaserHub.Devices[i], LaserHub.MainFrame)}, 5);
                }
            }
        }


        private void SaveObjStgBtn_Click(object sender, RoutedEventArgs e)
        {
            AppSt.Default.default_scale_x = ScaleXBox.Value.Value;
            AppSt.Default.default_scale_y = ScaleYBox.Value.Value;
            AppSt.Default.default_angle = AngleBox.Value.Value;
            AppSt.Default.default_mirror = MirrorBox.IsChecked.Value;
            AppSt.Default.stg_scale_invert = ScaleInvertCheck.IsChecked.Value;
            AppSt.Default.stg_scale_percent = ScalePercentCheck.IsChecked.Value;
            AppSt.Default.object_solid = SolidObject.IsChecked.Value;
            AppSt.Default.stg_show_name = ShowNameCheck.IsChecked.Value;
            AppSt.Default.stg_selectable_show = SelectableShowCheck.IsChecked.Value;
            AppSt.Default.Save();
        }

        private void CalibrationFormCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CalibrationFormCombo.SelectedValue != null)
            {
                LDeviceMesh.ClbrForm = (CalibrationForm)CalibrationFormCombo.SelectedValue;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            LaserHub.Save(AppSt.Default.cl_moncha_path);
            AppSt.Default.Save();
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
                            ProgressPanel.SetProgressBar(1, 2, "Save Moncha");
                            LaserHub.Save(saveFileDialog.FileName);
                            if (File.Exists(saveFileDialog.FileName) == false)
                            {
                                ProgressPanel.SetProgressBar(2, 2,"Not Save");
                                SaveConfiguration(true);
                            }
                            else
                            {
                                ProgressPanel.SetProgressBar(2, 2, "Saved");
                                AppSt.Default.cl_moncha_path = saveFileDialog.FileName;
                            }                          
                        }
                    }
                    else
                    {
                        LaserHub.Save(AppSt.Default.cl_moncha_path);
                    }
                    MonchaPathBox.Content = AppSt.Default.cl_moncha_path;
                    AppSt.Default.Save();
                    ProgressPanel.End();
                    return false;
                    break;
                case MessageBoxResult.No:
                    ProgressPanel.End();
                    return false;
                    break;
                case MessageBoxResult.Cancel:
                    ProgressPanel.End();
                    return true;
                    break;

            }
            ProgressPanel.SetProgressBar(2, 2, "Save Setting");

            ProgressPanel.End();
            return false;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoadMoncha();
        }

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



        private void RectBtn_Click(object sender, RoutedEventArgs e) => MainCanvas.MouseAction = Panels.CanvasPanel.MouseAction.Mask;

        private void Line_Click(object sender, RoutedEventArgs e) => MainCanvas.MouseAction = Panels.CanvasPanel.MouseAction.Line;

        private void TcpListenBtn_Click(object sender, RoutedEventArgs e)
        {
            UdpListenerRun();
        }

        private void UdpListenerRun()
        {
                this.UDPLaserListener = new UdpLaserListener(AppSt.Default.ether_udp_port);
                UDPLaserListener.Run();

            LogBox.Items.Add($"Status Listener: {this.UDPLaserListener.Status}");
        }
        private void UdpListnerStop()
        {
            this.UDPLaserListener.Stop();
        }

        private async void UdpLaserListener_IncomingData(object sender, byte[] e)
        {
            LogBox.Items.Add($"Incomin Data: {string.Join(string.Empty, e)}");
            int position = 0;

            if (e != null)
            {
                try
                {
                    CadObjectsGroup geometries = new CadObjectsGroup(await GCByteReader.Read(e, "Ethernet"));

                    ContourScrollPanel.Add(false, geometries, geometries.Name);

                }
                catch
                {
                    LogBox.Items.Add($"Incoming bullshit");
                    Console.WriteLine("Incoming bullshit");
                }
            }
        }

        private void ethernetToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
            {
                if (this.UDPLaserListener == null)
                {
                    UdpListenerRun();
                }
                else
                {
                    if (this.UDPLaserListener.Status == false)
                    {
                        UdpListenerRun();
                    }
                    else UdpListnerStop();
                }
            }
            
        }

        private void PortUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            AppSt.Default.ether_udp_port = (int)PortUpDn.Value.Value;
            AppSt.Default.Save();
        }

        private void AttachRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.DataContext != null)
            {
                AppSt.Default.stg_default_position = radioButton.DataContext.ToString();
            }
        }

        private void LincenseItem_Click(object sender, RoutedEventArgs e)
        {
            RequestLicenseCode requestLicenseCode = new RequestLicenseCode() { DataContext = LaserHub.lockKey };
            requestLicenseCode.ShowDialog();
        }


    }

    public class MultiObjectList : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
           return values;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { value };
        }
    }

    public class PlayBackConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value == true ? Brushes.YellowGreen : Brushes.Yellow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
