using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ToGeometryConverter;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels.RightPanel
{
    /// <summary>
    /// Логика взаимодействия для WorkFolderPanel.xaml
    /// </summary>
    public partial class WorkFolderPanel : UserControl
    {
        private MainWindow mainWindow => (MainWindow)this.DataContext;

        public WorkFolderPanel()
        {
            InitializeComponent();
        }

        private void WorkFolderRefreshBtn_Click(object sender, RoutedEventArgs e) => RefreshWorkFolderList();

        private void RefreshWorkFolderList()
        {
            if (Directory.Exists(AppSt.Default.save_work_folder) == true)
            {
                List<string> paths = new List<string>();
                foreach (string path in Directory.GetFiles(AppSt.Default.save_work_folder))
                {
                    string format = path.Split('.').Last();

                    if (ToGC.Filter.Contains($"*.{format};") == true)
                    {
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

        private async void TextBlock_MouseDownAsync(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                await mainWindow.OpenFile($"{AppSt.Default.save_work_folder}\\{WorkFolderListBox.SelectedItem.ToString()}");
            }
        }

        private void Border_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.ContextMenu.DataContext is MenuItem cmindex)
                {
                    switch (cmindex.Tag)
                    {
                        case "OpenEditor":
                            System.Diagnostics.Process.Start($"{AppSt.Default.save_work_folder}\\{WorkFolderListBox.SelectedItem.ToString()}");
                            break;
                        case "OpenFolder":
                            System.Diagnostics.Process.Start(AppSt.Default.save_work_folder);
                            break;
                    }
                }
            }
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject obj = sender as DependencyObject;

            while (obj != null && !(obj is ContextMenu))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            (obj as ContextMenu).DataContext = sender;
        }
    }
}
