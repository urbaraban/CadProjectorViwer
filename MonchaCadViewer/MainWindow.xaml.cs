using MahApps.Metro.Controls;
using Microsoft.Win32;
using CadProjectorViewer.Calibration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AppSt = CadProjectorViewer.Properties.Settings;
using System.Linq;
using System.Diagnostics;
using KompasLib.Tools;
using CadProjectorViewer.CanvasObj;
using System.Globalization;
using CadProjectorSDK;
using CadProjectorSDK.Device;
using CadProjectorSDK.CadObjects;
using KompasLib.KompasTool;
using System.Windows.Media.Media3D;
using StclLibrary.Laser;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.Windows.Data;
using CadProjectorViewer.Panels;
using ToGeometryConverter;
using ToGeometryConverter.Object;
using System.Threading.Tasks;
using ToGeometryConverter.Object.Elements;
using ToGeometryConverter.Format.ILDA;
using System.Text;
using System.Net;
using CadProjectorViewer.Panels.CanvasPanel;
using System.Management;
using ToGeometryConverter.Format;
using CadProjectorViewer.StaticTools;
using System.Reflection;
using CadProjectorSDK.Tools;
using System.Windows.Media.Imaging;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Scenes;
using System.Threading;
using CadProjectorSDK.UDP;
using Microsoft.Xaml.Behaviors.Core;

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private KmpsAppl kmpsAppl;
        private bool inverseToggle = true;

        public ProjectorHub ProjectorHub { get; set; } = new ProjectorHub();

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

            ProgressPanel.Label = "Loaded";

            ProjectorHub.Log = PostLog;
            ProjectorHub.SetProgress = ProgressPanel.SetProgressBar;

            GCTools.Log = PostLog;
            GCTools.SetProgress = ProgressPanel.SetProgressBar;
            this.Title = $"CUT — Viewer v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            FileLoad.LoadMoncha(ProjectorHub, false);

            HotKeysManager.KeyActions.Add(new KeyAction()
            {
                Keys = new Key[] { Key.Escape },
                GetAction = ProjectorHub.ScenesCollection.MainScene.Break
            });
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

        private void PostLog(string msg) 
        {
            LogBox.Dispatcher.Invoke(() => { 
            LogBox.Items.Add(msg);
            });
        }


      





        private void Window_Closed(object sender, System.EventArgs e)
        {
            ProjectorHub.Play = false;
            ProjectorHub.Disconnect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            ProjectorHub.Play = !ProjectorHub.Play;
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
                CadGroup cadGeometries = 
                    new CadGroup(
                        await ContourCalc.GetGeometry(this.kmpsAppl.Doc, ProjectorHub.ProjectionSetting.PointStep.MX, false, true),
                        this.kmpsAppl.Doc.D7.Name);
                cadGeometries.UpdateTransform(AppSt.Default.Attach);
                ProjectorHub.ScenesCollection.Add(new ProjectionScene(cadGeometries));
            }
        }

        private async void kmpsAddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                GCCollection gCObjects = new GCCollection(this.kmpsAppl.Doc.D7.Name);

                gCObjects.Add(new GeometryElement(await ContourCalc.GetGeometry(this.kmpsAppl.Doc, ProjectorHub.ProjectionSetting.PointStep.MX, true, true), "Kompas"));

                CadGroup cadGeometries =
                      new CadGroup(
                          await ContourCalc.GetGeometry(this.kmpsAppl.Doc, ProjectorHub.ProjectionSetting.PointStep.MX, true, true),
                          this.kmpsAppl.Doc.D7.Name);

                ProjectorHub.ScenesCollection.Add(new ProjectionScene(cadGeometries));
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

           // HotKeysManager.RunAsync(new Key[] { e.Key });

            double _mult = Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 1;
            double step = ProjectorHub.ScenesCollection.MainScene.Movespeed;

            switch (e.Key)
            {
                case Key.Q:
                    ProjectorHub.ScenesCollection.MainScene.SelectNext();
                    break;
                case Key.W:
                    ProjectorHub.ScenesCollection.MainScene.MoveSelect(0, -step * _mult);
                    break;
                case Key.S:
                    ProjectorHub.ScenesCollection.MainScene.MoveSelect(0, step * _mult);
                    break;
                case Key.A:
                    ProjectorHub.ScenesCollection.MainScene.MoveSelect(-step * _mult, 0);
                    break;
                case Key.D:
                    ProjectorHub.ScenesCollection.MainScene.MoveSelect(step * _mult, 0);
                    break;
                case Key.F:
                    ProjectorHub.ScenesCollection.MainScene.Fix();
                    break;
                case Key.OemPlus:
                    break;
                case Key.Delete:
                    //ProjectorHub.ScenesCollection.MainScene.SelectedObject.Remove();
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
                    ProjectorHub.ScenesCollection.MainScene.SceneAction = SceneAction.NoAction;
                    break;
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e) => ProjectorHub.ScenesCollection.MainScene.Clear();

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
            string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
            if (filePaths != null)
            {
                foreach (string fileLoc in filePaths)
                { //переберает всю инфу пока не найдет строку адреса
                    if (File.Exists(fileLoc))
                    {
                        //OpenBtn.Content = "Открыть " + fileLoc.Split('\\').Last();
                    }
                }
            }
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

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = SaveConfiguration(false);
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            this.Close();
        }

        private void MenuSaveBtn_Click(object sender, RoutedEventArgs e) => SaveConfiguration(false);  

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }



        /// <summary>
        /// Save frame to ILDA file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ILDASaveBtn_Click(object sender, RoutedEventArgs e)
        {
          /*  SaveFileDialog saveFileDialog = new SaveFileDialog();
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
                for (int i = 0; i < ProjectorHub.Devices.Count; i++)
                {
                    ildaWriter.Write(($"{saveFileDialog.FileName.Replace(".ild", string.Empty)}_{i}.ild"), new List<LFrame>(){ await ProjectorHub.Devices[i].ReadyFrame.GetLFrame(ProjectorHub.Devices[i], ProjectorHub.MainFrame)}, 5);
                }
            }*/
        }



        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ProjectorHub.Save(AppSt.Default.cl_moncha_path);
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
                            ProjectorHub.Save(saveFileDialog.FileName);
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
                        ProjectorHub.Save(AppSt.Default.cl_moncha_path);
                    }
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



        private void RectBtn_Click(object sender, RoutedEventArgs e) => ProjectorHub.ScenesCollection.MainScene.SceneAction = SceneAction.Mask;

        private void Line_Click(object sender, RoutedEventArgs e) => ProjectorHub.ScenesCollection.MainScene.SceneAction = SceneAction.Line;

        private void TcpListenBtn_Click(object sender, RoutedEventArgs e) => ProjectorHub.UdpListenerRun(AppSt.Default.ether_udp_port);


        private void ethernetToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
            {
                if (toggle.IsOn == true)
                {
                    ProjectorHub.UdpListenerRun(AppSt.Default.ether_udp_port);
                }
                else 
                {
                    ProjectorHub.UdpListnerStop();
                }
            }
        }

        private void PortUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            AppSt.Default.ether_udp_port = (int)PortUpDn.Value.Value;
            AppSt.Default.Save();
        }



        private void LincenseItem_Click(object sender, RoutedEventArgs e)
        {
            RequestLicenseCode requestLicenseCode = new RequestLicenseCode() { DataContext = ProjectorHub.lockKey };
            requestLicenseCode.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BrowseMWSItem_Click(object sender, RoutedEventArgs e) => FileLoad.LoadMoncha(ProjectorHub, true);



        private ActionCommand pasteCommand;
        public ICommand PasteCommand => pasteCommand ??= new ActionCommand(Paste);

        private async void Paste()
        {
            var text = Clipboard.GetData(DataFormats.Text) as string;
            try
            {
                ProjectorHub.ScenesCollection.Add(await FileLoad.GetCliboard(text));
            }
            catch
            {
                LogBox.Items.Add("Clipboard is not geometry");
            }
        }

        private ActionCommand openCommand;
        public ICommand OpenCommand => openCommand ??= new ActionCommand(Open);

        private async void Open()
        {
            WinForms.OpenFileDialog openFile = new WinForms.OpenFileDialog();
            string filter = FileLoad.GetFilter();
            openFile.Filter = filter;
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
                if (await FileLoad.GetFilePath(openFile.FileName) is ProjectionScene scene)
                {
                    ProjectorHub.ScenesCollection.Add(scene);
                }
            }
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

    public class LicenceColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b == true) return Brushes.Green;
            else return Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class AttachConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value == (string)parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value == true ? parameter : AppSt.Default.Attach;
        }
    }

}
