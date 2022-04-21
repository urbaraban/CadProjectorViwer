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
using CadProjectorSDK.Tools.ILDA;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using CadProjectorSDK.UDP.Scenario;
using CadProjectorViewer.Panels.RightPanel;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorSDK.Scenes.Actions;
using MonchaCadViewer.Config;
using CadProjectorSDK.SaveMWS;

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public delegate void Logging(string message, string sender);
        public static Logging Log;


        public delegate void Progress(int position, int max, string message);
        public static Progress SetProgress;


        private KmpsAppl kmpsAppl;
        private bool inverseToggle = true;

        public ProjectorHub ProjectorHub 
        {
            get => projectorHub;
            set
            {
                projectorHub = value;
                this.DataContext = value;
                OnPropertyChanged("ProjectorHub");
            }
        } 
        private ProjectorHub projectorHub = new ProjectorHub(AppSt.Default.cl_moncha_path);

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

            Log = LogPanel.Logs.PostLog;
            SetProgress = ProgressPanel.SetProgressBar;

            ProjectorHub.Log = LogPanel.Logs.PostLog;
            ProjectorHub.SetProgress = ProgressPanel.SetProgressBar;

            GCTools.Log = LogPanel.Logs.PostLog;
            GCTools.SetProgress = ProgressPanel.SetProgressBar;

            SetProgress?.Invoke(1, 1, "Loaded");

            this.Title = $"CUT — Viewer v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";

            HotKeysManager.KeyActions.Add(new KeyAction()
            {
                Keys = new Key[] { Key.Escape },
                GetAction = ProjectorHub.ScenesCollection.SelectedScene.Break
            });

            projectorHub.UDPLaserListener.OutFilePathWorker = FileLoad.GetUDPString;

            if (AppSt.Default.udp_auto_run == true)
            {
                projectorHub.UDPLaserListener.Run(AppSt.Default.ether_udp_port);
            }

            if (this.Height > SystemParameters.FullPrimaryScreenHeight * 0.9) this.Height = SystemParameters.FullPrimaryScreenHeight * 0.9;
            if (this.Width > SystemParameters.FullPrimaryScreenWidth * 0.9) this.Width = SystemParameters.FullPrimaryScreenWidth * 0.9;
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

      
        private void Window_Closed(object sender, System.EventArgs e)
        {
            ProjectorHub.Disconnect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }



        private void kmpsConnectToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI == null)
            {
                kmpsAppl = new KmpsAppl();

                if (kmpsAppl.Connect())
                {
                    kmpsAppl.ConnectBoolEvent += KmpsAppl_ConnectBoolEvent;

                    kmpsAppl.AppEvent.DocumentOpened += KmpsAppl_OpenedDoc;

                    kmpsConnectToggle.IsOn = KmpsAppl.KompasAPI != null;
                }

            }
        }

        private void KmpsAppl_OpenedDoc(object newDoc, int docType)
        {
            KmpsNameLbl.Invoke(() => { 
            if (newDoc is KmpsDoc kmpsDoc)
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
            /*if (KmpsAppl.KompasAPI != null)
            {
                CadGroup cadGeometries = 
                    new CadGroup(
                        await ContourCalc.GetGeometry(this.kmpsAppl.Doc, ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value, false, true),
                        this.kmpsAppl.Doc.D7.Name);

                SceneTask sceneTask = new SceneTask()
                {
                    Object = cadGeometries,
                    TableID = projectorHub.ScenesCollection.SelectedScene.TableID,
                    Command = new List<string>()
                        {
                            "CLEAR",
                            "ALIGN",
                            "SHOW",
                            "PLAY"
                        }
                };
                projectorHub.ScenesCollection.AddTask(sceneTask);
            }*/
        }

        private async void kmpsAddBtn_Click(object sender, RoutedEventArgs e)
        {
           /* if (KmpsAppl.KompasAPI != null)
            {
                GCCollection gCObjects = new GCCollection(this.kmpsAppl.Doc.D7.Name);

                gCObjects.Add(new GeometryElement(await ContourCalc.GetGeometry(this.kmpsAppl.Doc, ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value, true, true), "Kompas"));

                CadGroup cadGeometries =
                      new CadGroup(
                          await ContourCalc.GetGeometry(this.kmpsAppl.Doc, ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value, true, true),
                          this.kmpsAppl.Doc.D7.Name);

                SceneTask sceneTask = new SceneTask()
                {
                    Object = cadGeometries,
                    TableID = projectorHub.ScenesCollection.SelectedScene.TableID,
                    Command = new List<string>()
                    { "CLEAR", "ALIGN", "SHOW", "PLAY" }
                };
                projectorHub.ScenesCollection.AddTask(sceneTask);
            }*/
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

           // HotKeysManager.RunAsync(new Key[] { e.Key });

            double _mult = Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 1;
            double step = ProjectorHub.ScenesCollection.SelectedScene.Movespeed;

            switch (e.Key)
            {
                case Key.W:
                    ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                        new MovingCommand(ProjectorHub.ScenesCollection.SelectedScene.SelectedObjects,
                        0, -step * _mult));
                    break;
                case Key.S:
                    ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                        new MovingCommand(ProjectorHub.ScenesCollection.SelectedScene.SelectedObjects,
                        0, step * _mult));
                    break;
                case Key.A:
                    ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                        new MovingCommand(ProjectorHub.ScenesCollection.SelectedScene.SelectedObjects,
                        -step * _mult, 0));
                    break;
                case Key.D:
                    ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                        new MovingCommand(ProjectorHub.ScenesCollection.SelectedScene.SelectedObjects,
                        step * _mult, 0));
                    break;
                case Key.Z:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.UndoLast();
                    }
                    break;
                case Key.OemPlus:
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
                    ProjectorHub.ScenesCollection.SelectedScene.Break();
                    break;
            }
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
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            this.ShowInTaskbar = false;
            ProjectorHub.Disconnect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
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
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "(*.ild)|*.ild| All Files (*.*)|*.*";
            saveFileDialog.FileName = "export";
            saveFileDialog.ShowDialog();
            IldaWriter ildaWriter = new IldaWriter();
            
            if (saveFileDialog.FileName != string.Empty)
            {
                LProjector[] devices = ProjectorHub.ScenesCollection.SelectedScene.Projectors.ToArray();
                for (int i = 0; i < devices.Length; i++)
                {
                    ildaWriter.Write(($"{saveFileDialog.FileName.Replace(".ild", string.Empty)}_{i}_{devices[i].IPAddress}.ild"), 
                        new List<LFrame>() { 
                            await LFrameConverter.SolidLFrame(devices[i].RenderObjects, devices[i], false) 
                        ?? new LFrame() } , 5);
                }
            }
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
                                AppSt.Default.cl_moncha_path = saveFileDialog.FileName;
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

        public ICommand MaskCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.AlreadyAction = new DrawMaskAction(ProjectorHub.ScenesCollection.SelectedScene.Size);
        });

        public ICommand LineCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.AlreadyAction = new DrawLineAction();
        });



        private void TcpListenBtn_Click(object sender, RoutedEventArgs e) => ProjectorHub.UDPLaserListener.Run(AppSt.Default.ether_udp_port);


        private void ethernetToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
            {
                if (toggle.IsOn == true)
                {
                    ProjectorHub.UDPLaserListener.Run(AppSt.Default.ether_udp_port);
                }
                else 
                {
                    ProjectorHub.UDPLaserListener.Stop();
                }
            }
        }



        private void BrowseMWSItem_Click(object sender, RoutedEventArgs e) => FileLoad.LoadMoncha(ProjectorHub, true);

        public ICommand ClosedCommand => new ActionCommand(() => {
            ProjectorHub.Disconnect();
            this.Close();
        });

        public ICommand Clear => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.Clear();
        });

        public ICommand FixSelectCommand => new ActionCommand(ProjectorHub.ScenesCollection.SelectedScene.Fix);

        public ICommand SelectNextCommand => new ActionCommand(()=> { ProjectorHub.ScenesCollection.SelectedScene.SelectNextObject(1); });

        public ICommand SelectPreviousCommand => new ActionCommand(() => { ProjectorHub.ScenesCollection.SelectedScene.SelectNextObject(-1); });

        public ICommand RefreshFrameCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.Refresh();
        });

        public ICommand HideToTray => new ActionCommand(() => {
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();
        });

        public ICommand ShowFromTray => new ActionCommand(() =>
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
            this.Topmost = true;
            this.Topmost = false;
        });

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
        }

        public ICommand ShowLicenceCommand => new ActionCommand(()=> {
            RequestLicenseCode requestLicenseCode = new RequestLicenseCode() { DataContext = ProjectorHub.lockKey };
            requestLicenseCode.ShowDialog();
        });

        public ICommand RemoveOtherAppCommand => new ActionCommand(RemoveOtherApp);

        private async void RemoveOtherApp()
        {
            string Name = AppDomain.CurrentDomain.FriendlyName;
            Name = Name.Substring(0, Name.LastIndexOf('.'));
            Process current = Process.GetCurrentProcess();
            
            var chromeDriverProcesses = Process.GetProcesses().
            Where(pr => pr.ProcessName == Name && pr.Id != current.Id); // without '.exe'

            Log?.Invoke($"Find {chromeDriverProcesses.Count()} run process", "APP");

            foreach (var process in chromeDriverProcesses)
            {
                process.Kill();
            }
        }

        public ICommand PasteCommand => new ActionCommand(Paste);

        private async void Paste()
        {
            try
            {
                SceneTask sceneTask = new SceneTask()
                {
                    TableID = this.ProjectorHub.ScenesCollection.SelectedScene.TableID,
                    Object = await FileLoad.GetCliboard()
                };
                ProjectorHub.ScenesCollection.LoadedObjects.Add(sceneTask);
            }
            catch
            {
                Log?.Invoke("Clipboard is not geometry", "APP");
            }
        }

        public ICommand PlayCommand => new ActionCommand(() => {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                this.projectorHub.ScenesCollection.SelectedScene.Play = !this.projectorHub.ScenesCollection.SelectedScene.Play;
            }
            else
            {
                PlayAllCommand.Execute(null);
            }
        });

        public ICommand PlayAllCommand => new ActionCommand(() =>
        {
            bool stat = !this.projectorHub.ScenesCollection.Any(sc => sc.Play);
            foreach (ProjectionScene scene in this.projectorHub.ScenesCollection)
            {
                scene.Play = stat;
            }
        });

        public ICommand DeleteCommand => new ActionCommand(() => {
            this.projectorHub.ScenesCollection.SelectedScene.RemoveRange(this.projectorHub.ScenesCollection.SelectedScene.SelectedObjects);
        });

        public ICommand SaveSceneCommand => new ActionCommand(() => {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "2CUT Scene (*.2scn)|*.2scn";
            if (saveFileDialog.ShowDialog() == true)
            {
                SaveScene.WriteXML(projectorHub.ScenesCollection.SelectedScene, saveFileDialog.FileName);
            }
           
        });

        public ICommand OpenSceneCommand => new ActionCommand(() => {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Moncha (.2scn)|*.2scn|All Files (*.*)|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                projectorHub.ScenesCollection.AddTask(new SceneTask(SaveScene.ReadXML(fileDialog.FileName)));
            }
        });


        public ICommand MakeNewWorkPlaceCommand => new ActionCommand(() => {
            this.ProjectorHub.Disconnect();
            this.ProjectorHub = new ProjectorHub(string.Empty);
            GC.Collect();
        });

        public ICommand OpenCommand => new ActionCommand(Open);

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
                if (await FileLoad.GetFilePath(openFile.FileName, projectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value) is UidObject Obj)
                {
                    SceneTask sceneTask = new SceneTask()
                    {
                        Object = Obj,
                        TableID = projectorHub.ScenesCollection.SelectedScene.TableID,
                        Command = new List<string>()
                        {
                            "CLEAR",
                            "ALIGN",
                            "SHOW",
                            "PLAY"
                        }
                    };
                    projectorHub.ScenesCollection.AddTask(sceneTask);
                }
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion



        private async void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is IConnected connected)
            {
                await connected.Reconnect();
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
