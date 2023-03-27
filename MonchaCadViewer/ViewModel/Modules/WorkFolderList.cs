using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorSDK;
using CadProjectorViewer.StaticTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using AppSt = CadProjectorViewer.Properties.Settings;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Xaml.Behaviors.Core;
using System.Windows.Input;
using ToGeometryConverter;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Reflection;
using System.Data;
using System.Globalization;

namespace CadProjectorViewer.ViewModel.Modules
{
    public class WorkFolderList : INotifyPropertyChanged
    {
        public event EventHandler<string> PathSelected;

        public List<GCFormat> Extensions { get; } = FileLoad.GetFormatList();
        public GCFormat SelectExtension 
        {
            get => _selectextensions;
            set
            {
                _selectextensions = value;
                OnPropertyChanged(nameof(SelectExtension));
                if (FilInfosCollection != null)
                {
                    FilInfosCollection.Filter = new Predicate<object>(Contains);
                }
            }
        }
        private GCFormat _selectextensions;

        public CollectionView FilInfosCollection 
        {
            get => fileSystemInfos;
            set
            {
                fileSystemInfos = value;
                OnPropertyChanged(nameof(FilInfosCollection));
            }
        }
        private CollectionView fileSystemInfos = new CollectionView(new List<FileSystemInfo>());

        public FileSystemInfo SelectedFileSystem 
        {
            get => _selectedFileSystem;
            set
            {
                _selectedFileSystem = value;
                OnPropertyChanged(nameof(SelectedFileSystem));
            }
        }
        private FileSystemInfo _selectedFileSystem;

        public string StringFilter 
        {
            get => _stringfilter;
            set
            {
                _stringfilter = value;
                OnPropertyChanged(nameof(StringFilter));
                if (FilInfosCollection != null)
                {
                    FilInfosCollection.Filter = new Predicate<object>(Contains);
                }
            }
        } 
        private string _stringfilter = string.Empty;

        public DirectoryInfo AlreadyDirectory
        {
            get => directoryInfo;
            set
            {
                directoryInfo = value;
                AppSt.Default.save_work_folder = value.FullName;
                AppSt.Default.Save();
                OnPropertyChanged(nameof(AlreadyDirectory));
                RefreshWorkFolderList(directoryInfo.FullName);
            }
        }
        private DirectoryInfo directoryInfo = new DirectoryInfo(AppSt.Default.save_work_folder);

        public WorkFolderList()
        {
            RefreshWorkFolderList();
        }

        private void RefreshWorkFolderList() => RefreshWorkFolderList(this.AlreadyDirectory.FullName);

        private void RefreshWorkFolderList(string Path)
        {
            SortDescriptionCollection sortDescriptions = FilInfosCollection.SortDescriptions;
            List<FileSystemInfo> fileSystemInfos = GetFolderItems(Path);
            CollectionView view = CollectionViewSource.GetDefaultView(fileSystemInfos) as CollectionView;
            FilInfosCollection = view;
            FilInfosCollection.Filter = new Predicate<object>(Contains);
            foreach (SortDescription sortDescription in sortDescriptions)
            {
                FilInfosCollection.SortDescriptions.Add(sortDescription);
            }
        }

        public List<FileSystemInfo> GetFolderItems(string Path)
        {
            List<FileSystemInfo> infos = new List<FileSystemInfo>();

            if (Directory.Exists(Path))
            {
                if (AlreadyDirectory.FullName != Path)
                    this.AlreadyDirectory = new DirectoryInfo(Path);

                infos.Add(this.AlreadyDirectory.Parent);

                foreach (string name in Directory.GetDirectories(Path))
                {
                    infos.Add(new DirectoryInfo(name));
                }

                foreach (string path in Directory.GetFiles(Path))
                {
                    string format = path.Split('.').Last();

                    if (FileLoad.GetFilter().Contains($"*.{format.ToLower()};") == true)
                    {
                        infos.Add(new FileInfoItem(path));
                    }
                }
            }

            return infos;
        }

        public ICommand SelectWorkFolderCommand => new ActionCommand(() =>
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                if (Directory.Exists(dialog.FileName) == true)
                {
                    AlreadyDirectory = new DirectoryInfo(dialog.FileName);
                }
            }
            RefreshWorkFolderList();
        });

        public ICommand SelectPathSendCommand (FileSystemInfo fileSystemInfo) => new ActionCommand(() =>
        {
            if (fileSystemInfo is DirectoryInfo directoryInfo)
            {
                this.AlreadyDirectory = directoryInfo;
            } 
            else
            {
                PathSelected?.Invoke(this, fileSystemInfo.FullName);
            }
        });

        public ICommand ClearFilterBoxCommand => new ActionCommand(() =>
        {
            StringFilter = string.Empty;
        });

        public ICommand RefreshListCommand => new ActionCommand(() => RefreshWorkFolderList());

        public bool Contains(object pt)
        {
            bool result = false;
            string FormatString = FileLoad.GetFilter();

            if (SelectExtension is GCFormat format)
                FormatString = string.Join(" ", format.ShortName);

            if (pt is FileSystemInfo fileInfo)
            {
                result =
                    fileInfo.Name.ToLower().Contains(StringFilter.ToLower())
                    && (FormatString.Contains(fileInfo.Extension));
            }
            else
                result = true;
            return result;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }

    public class FileInfoItem : FileSystemInfo
    {
        public FileInfoItem(string path)
        {
            this.FullPath = path;
        }

        public string FullName => this.FullPath;

        public override string Name => Path.GetFileNameWithoutExtension(FullName);

        public override bool Exists => File.Exists(FullName);

        public string Extension => Path.GetExtension(FullName).ToLower() ?? string.Empty;

        public override void Delete()
        {
            
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll")]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

        [DllImport("shell32.dll")]
        private static extern void ILFree(IntPtr pidl);

        public ICommand OpenFolderCommand => new ActionCommand(() =>
        {
            IntPtr pidl = ILCreateFromPathW(this.FullName);
            SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
            ILFree(pidl);
        });

        public ICommand OpenEditorCommand => new ActionCommand(() =>
        {
            System.Diagnostics.Process.Start(this.FullName);
        });
    }
}
