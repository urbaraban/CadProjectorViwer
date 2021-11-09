using CadProjectorSDK;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.StaticTools;
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
        private ProjectorHub projectorHub => (ProjectorHub)this.DataContext;

        public WorkFolderPanel()
        {
            InitializeComponent();
            List<GCFormat> formats = FileLoad.GetFormatList();
            ComboFilter.ItemsSource = formats;
            ComboFilter.SelectedItem = formats[0];
            RefreshWorkFolderList();
        }

        private void WorkFolderRefreshBtn_Click(object sender, RoutedEventArgs e) => RefreshWorkFolderList();

        private void RefreshWorkFolderList()
        {
            if (Directory.Exists(AppSt.Default.save_work_folder) == true)
            {
                List<FileInfo> infos = new List<FileInfo>();
                foreach (string path in Directory.GetFiles(AppSt.Default.save_work_folder))
                {
                    string format = path.Split('.').Last();

                    if (FileLoad.GetFilter().Contains($"*.{format};") == true)
                    {
                        infos.Add(new FileInfo(path));
                    }
                }
                WorkFolderListBox.ItemsSource = infos;
                CollectionView view = CollectionViewSource.GetDefaultView(WorkFolderListBox.ItemsSource) as CollectionView;
                WorkFolderListBox.Items.Filter = new Predicate<object>(Contains);
            }
        }

        public bool Contains(object pt)
        {
            string FormatString = FileLoad.GetFilter();

            if (ComboFilter.SelectedItem is GCFormat format) FormatString = string.Join(" ", format.ShortName);

            if (pt is FileInfo fileInfo)
            {
                return (fileInfo.Filename.ToLower().Contains(WorkFolderFilter.Text.ToLower()) && (FormatString.Contains(fileInfo.Fileformat)));
            }
            return false;
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
            if (WorkFolderListBox.SelectedItem is FileInfo fileInfo)
            {
                if (await FileLoad.GetFilePath(fileInfo.Filepath) is UidObject Obj)
                {
                    projectorHub.ScenesCollection.LoadedObject.Add(Obj);
                }
            }

        }

        private void Border_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is FileInfo fileInfo)
            {
                if (item.ContextMenu.DataContext is MenuItem cmindex)
                {
                    switch (cmindex.Tag)
                    {
                        case "OpenEditor":
                            System.Diagnostics.Process.Start(fileInfo.Filepath);
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

        private void ComboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshWorkFolderList();
        }
    }

    internal struct FileInfo
    {
        public string Filepath { get; }
        public string Filename { get; }
        public string Fileformat { get; }
        public int Filesize { get; }
        
        public FileInfo(string Filepath)
        {
            this.Filepath = Filepath;
            this.Filename = Filepath.Split('\\').Last().Split('.').First();
            this.Fileformat = Filepath.Split('.').Last();
            this.Filesize = 0;
        }
    }
}
