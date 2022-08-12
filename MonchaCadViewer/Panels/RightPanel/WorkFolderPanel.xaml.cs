using CadProjectorSDK;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.StaticTools;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        private string alreadypath;

        public WorkFolderPanel()
        {
            InitializeComponent();
            List<GCFormat> formats = FileLoad.GetFormatList();
            ComboFilter.ItemsSource = formats;
            ComboFilter.SelectedItem = formats[0];
            RefreshWorkFolderList(AppSt.Default.save_work_folder);
        }

        private void WorkFolderRefreshBtn_Click(object sender, RoutedEventArgs e) => RefreshWorkFolderList(alreadypath);

        private void RefreshWorkFolderList(string Path)
        {
            if (Directory.Exists(Path) == true)
            {
                List<FileInfo> infos = new List<FileInfo>();


                if (Path != this.alreadypath)
                    this.alreadypath = Path;

                foreach (string name in Directory.GetDirectories(Path))
                {
                    infos.Add(new FileInfo(name, true));
                }

                foreach (string path in Directory.GetFiles(Path))
                {
                    string format = path.Split('.').Last();

                    if (FileLoad.GetFilter().Contains($"*.{format.ToLower()};") == true)
                    {
                        infos.Add(new FileInfo(path, false));
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
                return 
                    fileInfo.Filename.ToLower().Contains(WorkFolderFilter.Text.ToLower()) 
                    && (FormatString.Contains(fileInfo.Fileformat)
                    || fileInfo.IsFolder == true);
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
                    RefreshWorkFolderList(AppSt.Default.save_work_folder);
                }
            }
        }

        private void WorkFolderFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WorkFolderListBox.Items.Count < 1)
            {
                RefreshWorkFolderList(alreadypath);
            }

            if (WorkFolderListBox.Items.Count == 1 && WorkFolderListBox.Items[0] is FileInfo fileInfo)
            {
                WorkFolderListBox.SelectedIndex = 0;
                SendTask(fileInfo);
            }

            CollectionViewSource.GetDefaultView(WorkFolderListBox.ItemsSource).Refresh();
        }

        private void WorkFolderFilter_GotFocus(object sender, RoutedEventArgs e) => WorkFolderFilter.SelectAll();

        private async void TextBlock_MouseDownAsync(object sender, MouseButtonEventArgs e)
        {
            if (WorkFolderListBox.SelectedItem is FileInfo fileInfo)
            {
                if (fileInfo.IsFolder == true)
                    RefreshWorkFolderList(fileInfo.Filepath);
                else
                    SendTask(fileInfo);
            }
        }

        private async void SendTask(FileInfo fileInfo)
        {
            if (await FileLoad.GetFilePath(fileInfo.Filepath, projectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value) is UidObject uidObject)
            {
                SceneTask sceneTask = new SceneTask()
                {
                    Object = uidObject,
                    TableID = projectorHub.ScenesCollection.SelectedScene.TableID,
                };
                projectorHub.ScenesCollection.AddTask(sceneTask);
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
                            OpenFolderAndSelectFile(fileInfo.Filepath);
                            break;
                    }
                }
            }
        }

        public static void OpenFolderAndSelectFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            IntPtr pidl = ILCreateFromPathW(filePath);
            SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
            ILFree(pidl);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll")]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

        [DllImport("shell32.dll")]
        private static extern void ILFree(IntPtr pidl);


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
            RefreshWorkFolderList(alreadypath);
        }
    }

    internal struct FileInfo
    {
        public string Filepath { get; }
        public string Filename { get; }
        public string Fileformat { get; }
        public int Filesize { get; }

        public bool IsFolder { get; }
        
        public FileInfo(string Filepath, bool isfolder)
        {
            this.IsFolder = isfolder;
            this.Filepath = Filepath;
            this.Filename = Filepath.Split('\\').Last();
            this.Fileformat = "folder";
            if (isfolder == false)
            {
                this.Fileformat = Filepath.Split('.').Last();
                this.Filename = this.Filename.Remove(this.Filename.Length - Fileformat.Length - 1);
            }
            this.Filesize = 0;
        }
    }
}
