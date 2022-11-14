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
using CadProjectorSDK.Config;
using CadProjectorSDK.Render.Graph;
using System.Xml.Linq;
using CadProjectorSDK.Render;
using CadProjectorViewer.ViewModel;

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private AppMainModel mainModel { get; } = new AppMainModel();

        private KmpsAppl kmpsAppl;

        public MainWindow()
        {
            InitializeComponent();

            SetLanguage();

            App.SetProgress?.Invoke(1, 1, "Loaded");

            this.Title = $"CUT — Viewer v{Assembly.GetExecutingAssembly().GetName().VersionCompatibility.ToString()}";

            this.DataContext = mainModel;

            HotKeysManager.KeyActions.Add(new KeyAction()
            {
                Keys = new Key[] { Key.Escape },
                GetAction = mainModel.ProjectorHub.ScenesCollection.SelectedScene.Break
            });

            if (this.Height > SystemParameters.FullPrimaryScreenHeight * 0.9) this.Height = SystemParameters.FullPrimaryScreenHeight * 0.9;
            if (this.Width > SystemParameters.FullPrimaryScreenWidth * 0.9) this.Width = SystemParameters.FullPrimaryScreenWidth * 0.9;

        }

        #region Language
        private void SetLanguage()
        {
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
        #endregion

        private void Window_Closed(object sender, System.EventArgs e)
        {
            mainModel.ProjectorHub.Disconnect();
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
            if (KmpsAppl.KompasAPI != null)
            {
                KmpsDoc doc = new KmpsDoc(KmpsAppl.Appl.ActiveDocument);

                CadGroup cadGeometries = 
                    new CadGroup(
                        await ContourCalc.GetGeometry(doc, mainModel.ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value, false, true),
                        doc.D7.Name);

                SceneTask sceneTask = new SceneTask()
                {
                    Object = cadGeometries,
                    TableID = mainModel.ProjectorHub.ScenesCollection.SelectedScene.TableID,
                };
                mainModel.ProjectorHub.ScenesCollection.AddTask(sceneTask);
            }
        }

        private async void kmpsAddBtn_Click(object sender, RoutedEventArgs e)
        {
           if (KmpsAppl.KompasAPI != null)
            {
                KmpsDoc doc = new KmpsDoc(KmpsAppl.Appl.ActiveDocument);

                GCCollection gCObjects = new GCCollection(doc.D7.Name);

                gCObjects.Add(new GeometryElement(await ContourCalc.GetGeometry(doc, mainModel.ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value, true, true), "Kompas"));

                CadGroup cadGeometries =
                      new CadGroup(
                          await ContourCalc.GetGeometry(doc, mainModel.ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value, true, true),
                          doc.D7.Name);

                SceneTask sceneTask = new SceneTask()
                {
                    Object = cadGeometries,
                    TableID = mainModel.ProjectorHub.ScenesCollection.SelectedScene.TableID,
                };
                mainModel.ProjectorHub.ScenesCollection.AddTask(sceneTask);
            }
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
                case Key.Q:
;
                    this.SelectNextCommand.Execute(this);
                    break;
                case Key.E:
                    this.SelectPreviousCommand.Execute(this);
                    break;
                case Key.OemPlus:
                    break;
                case Key.D1:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(2);
                    break;
                case Key.D2:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(3);
                    break;
                case Key.D3:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(4);
                    break;
                case Key.D4:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(5);
                    break;
                case Key.D5:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(6);
                    break;
                case Key.D6:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(7);
                    break;
                case Key.D7:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(8);
                    break;
                case Key.D8:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(9);
                    break;
                case Key.D9:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(10);
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
            mainModel.ProjectorHub.Disconnect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            base.OnClosed(e);
            this.Close();
        }

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
                LProjector[] devices = mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.ToArray();
                for (int i = 0; i < devices.Length; i += 1)
                {
                    IList<IRenderedObject> elements = GraphExtensions.SolidVectors(devices[i].RenderObjects, devices[i]);
                    if (devices[i].Optimized == true && elements.Count > 0)
                    {
                        //vectorLines = VectorLinesCollection.Optimize(vectorLines);
                        elements = GraphExtensions.FindShortestCollection(
                            elements, devices[i].ProjectionSetting.PathFindDeep, devices[i].ProjectionSetting.FindSolidElement);
                    }
                    var vectorLine = GraphExtensions.GetVectorLines(elements);

                    ildaWriter.Write(($"{saveFileDialog.FileName.Replace(".ild", string.Empty)}_{i}_{devices[i].IPAddress}.ild"), 
                        new List<LFrame>() { 
                            LFrameConverter.SolidLFrame(vectorLine, devices[i]) 
                        ?? new LFrame() } , 5);
                }
            }
        }

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

        public ICommand ClosedCommand => new ActionCommand(() => {
            mainModel.ProjectorHub.Disconnect();
            this.Close();
        });
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
