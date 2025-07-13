using CadProjectorSDK.Scenes.Commands;
using CadProjectorSDK.Tools;
using CadProjectorViewer.Opening;
using CadProjectorViewer.Panels.RightPanel;
using CadProjectorViewer.Services;
using CadProjectorViewer.ViewModel;
using MahApps.Metro.Controls;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public delegate void Logging(string message, string sender);
        public static Logging Log;
        private AppMainModel mainModel { get; } = new AppMainModel();

        public MainWindow()
        {
            InitializeComponent();

            LanguageSet();
            this.DataContext = mainModel;

            HotKeysManager.KeyActions.Add(new KeyAction()
            {
                Keys = new Key[] { Key.Escape },
                GetAction = mainModel.ProjectorHub.ScenesCollection.SelectedScene.Break
            });

            if (this.Height > SystemParameters.FullPrimaryScreenHeight * 0.9) 
                this.Height = SystemParameters.FullPrimaryScreenHeight * 0.9;
            if (this.Width > SystemParameters.FullPrimaryScreenWidth * 0.9) 
                this.Width = SystemParameters.FullPrimaryScreenWidth * 0.9;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Title = $"CUT — Viewer v{version.Build.ToString()}.{version.MinorRevision.ToString()}";

            App.SetProgress?.Invoke(1, 1, "Loaded");
        }

        #region Language
        private void LanguageSet()
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

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

           // HotKeysManager.RunAsync(new Key[] { e.Key });

            double _mult = Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 1;
            _mult = Keyboard.Modifiers == ModifierKeys.Control ? 0.1 : _mult;
            double step = mainModel.ProjectorHub.ScenesCollection.SelectedScene.Movespeed;

            switch (e.Key)
            {
                case Key.NumPad8:
                case Key.W:
                    mainModel.ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                        new MovingCommand(mainModel.ProjectorHub.ScenesCollection.SelectedScene.SelectedObjects,
                        0, -step * _mult));
                    break;
                case Key.NumPad5:
                case Key.S:
                    mainModel.ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                        new MovingCommand(mainModel.ProjectorHub.ScenesCollection.SelectedScene.SelectedObjects,
                        0, step * _mult));
                    break;
                case Key.NumPad4:
                case Key.A:
                    mainModel.ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                        new MovingCommand(mainModel.ProjectorHub.ScenesCollection.SelectedScene.SelectedObjects,
                        -step * _mult, 0));
                    break;
                case Key.NumPad6:
                case Key.D:
                    mainModel.ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                        new MovingCommand(mainModel.ProjectorHub.ScenesCollection.SelectedScene.SelectedObjects,
                        step * _mult, 0));
                    break;
                case Key.Z:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        mainModel.ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.UndoLast();
                    }
                    break;
                case Key.NumPad7:
                case Key.Q:
                    mainModel.SelectNextCommand.Execute(this);
                    break;
                case Key.NumPad9:
                case Key.E:
                    mainModel.SelectPreviousCommand.Execute(this);
                    break;
                case Key.OemPlus:
                    break;
                case Key.D1:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(2);
                    break;
                case Key.D2:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(3);
                    break;
                case Key.D3:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(4);
                    break;
                case Key.D4:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(5);
                    break;
                case Key.D5:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(6);
                    break;
                case Key.D6:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(7);
                    break;
                case Key.D7:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(8);
                    break;
                case Key.D8:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(9);
                    break;
                case Key.D9:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.SelectedItem.DeviceBright(10);
                    break;
                case Key.Escape:
                    mainModel.ProjectorHub.ScenesCollection.SelectedScene.Break();
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
        private void ILDASaveBtn_Click(object sender, RoutedEventArgs e)
        {
            FileSave.ILDASave(this.mainModel.ProjectorHub.ScenesCollection.SelectedScene.Projectors.ToArray());
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

        private void ProgressPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.mainModel.Adminclick += 1;
        }

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileLoad.OpenFolderAndFocusFile(AppSt.Default.cl_moncha_path);
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
            if (value is LockKeysManager keysManager && keysManager.SelectMashineKey() is LockKey lockKey)
            {
                return lockKey.DaysLeft < 30 ? Brushes.Yellow : Brushes.Green;
            }
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
            return parameter;
        }
    }

    public class GetHubPageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppMainModel mainModel)
            {
                return new HubPage()
                {
                    DataContext = mainModel
                };
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
