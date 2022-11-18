using CadProjectorSDK;
using CadProjectorViewer.Panels.RightPanel;
using CadProjectorViewer.Panels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AppSt = CadProjectorViewer.Properties.Settings;
using CadProjectorViewer.StaticTools;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using Microsoft.Xaml.Behaviors.Core;
using System.Windows;
using System.Windows.Input;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorSDK.Scenes.Actions;
using System.IO;
using Microsoft.Win32;
using CadProjectorSDK.Config;
using ToGeometryConverter;
using CadProjectorViewer.ViewModel.Modules;
using System.Diagnostics;
using CadProjectorViewer.TCPServer;
using CadProjectorViewer.Dialogs;
using CadProjectorViewer.ToCommands;

namespace CadProjectorViewer.ViewModel
{
    public class AppMainModel : INotifyPropertyChanged
    { 
        public ToCUTServer CUTServer { get; }

        public bool AdminMode => Debugger.IsAttached == true || Adminclick > 9;
        public int Adminclick 
        {
            get => _adminclick;
            set
            {
                _adminclick = value;
            }
        }
        private int _adminclick = 0;

        public ProjectorHub ProjectorHub
        {
            get => projectorHub;
            set
            {
                projectorHub = value;
                OnPropertyChanged("ProjectorHub");
            }
        }
        private ProjectorHub projectorHub = new ProjectorHub(AppSt.Default.cl_moncha_path);

        public WorkFolderList WorkFolder { get; } = new WorkFolderList();

        public LogList Logs { get; } = new LogList(string.Empty);


        public AppMainModel()
        {
            App.Log = Logs.PostLog;
            App.SetProgress = ProgressPanel.SetProgressBar;

            ProjectorHub.Log = Logs.PostLog;
            ProjectorHub.SetProgress = ProgressPanel.SetProgressBar;

            GCTools.Log = Logs.PostLog;
            GCTools.SetProgress = ProgressPanel.SetProgressBar;

            projectorHub.UDPLaserListener.OutFilePathWorker = FileLoad.GetUDPString;

            if (AppSt.Default.udp_auto_run == true)
            {
                projectorHub.UDPLaserListener.Run(AppSt.Default.ether_udp_port);
            }

            WorkFolder.PathSelected += WorkFolder_PathSelected;

            this.CUTServer = new ToCUTServer(this);
        }

        private void CUTServer_IncomingMessage(object sender, string e)
        {
            throw new NotImplementedException();
        }

        private async void WorkFolder_PathSelected(object sender, string e) => OpenGeometryFile(e);

        public ICommand SaveCommand => new ActionCommand(() => SaveConfiguration(false));

        public ICommand SaveAsCommand => new ActionCommand(() => SaveConfiguration(true));

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
                                ProgressPanel.SetProgressBar(2, 2, "Not Save");
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

        public ICommand UDPToggleCommand => new ActionCommand(() =>
        {
            if (ProjectorHub.UDPLaserListener.Status == false)
            {
                ProjectorHub.UDPLaserListener.Run(AppSt.Default.ether_udp_port);
            }
            else
            {
                ProjectorHub.UDPLaserListener.Stop();
            }
        });

        public ICommand LoadMWSCommand => new ActionCommand(() => FileLoad.LoadMoncha(ProjectorHub, false));

        public ICommand Clear => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.Clear();
        });

        public ICommand SelectNextCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                        new SelectNextCommand(true, ProjectorHub.ScenesCollection.SelectedScene));
        });

        public ICommand SelectPreviousCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                        new SelectNextCommand(false, ProjectorHub.ScenesCollection.SelectedScene));
        });

        public ICommand DeleteCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.RemoveRange(ProjectorHub.ScenesCollection.SelectedScene.SelectedObjects);
        });

        public ICommand UndoCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.UndoLast();
        });

        public ICommand ShowLicenceCommand => new ActionCommand(() => {
            RequestLicenseCode requestLicenseCode = new RequestLicenseCode() { DataContext = ProjectorHub.LockKey };
            requestLicenseCode.ShowDialog();
        });

        public ICommand RemoveOtherAppCommand => new ActionCommand(App.RemoveOtherApp);

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
                App.Log?.Invoke("Clipboard is not geometry", "APP");
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

        public ICommand SaveSceneCommand => new ActionCommand(() => {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "2CUT Scene (*.2scn)|*.2scn";
            if (saveFileDialog.ShowDialog() == true)
            {
                FileSave.SaveScene(projectorHub.ScenesCollection.SelectedScene, saveFileDialog.FileName);
                //SaveScene.WriteXML(projectorHub.ScenesCollection.SelectedScene, saveFileDialog.FileName);
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


        public ICommand ShowTCPDialogCommand => new ActionCommand(() => {
            ManipulatorTCPDialog manipulatorTCP = new ManipulatorTCPDialog(this.CUTServer);
            manipulatorTCP.Show();
        });

        private async void Open()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            string filter = FileLoad.GetFilter();
            openFile.Filter = filter;
            if (AppSt.Default.save_work_folder == string.Empty)
            {
                System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    AppSt.Default.save_work_folder = folderDialog.SelectedPath;
                    AppSt.Default.Save();
                }
            }

            openFile.InitialDirectory = AppSt.Default.save_work_folder;
            openFile.FileName = null;
            if (openFile.ShowDialog() == true)
            {
                OpenGeometryFile(openFile.FileName);
            }
        }

        private async void OpenGeometryFile(string path)
        {
            if (await FileLoad.GetFilePath(path, this.ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value) is UidObject uidObject)
            {
                SceneTask sceneTask = new SceneTask()
                {
                    Object = uidObject,
                    TableID = projectorHub.ScenesCollection.SelectedScene.TableID,
                };
                this.ProjectorHub.ScenesCollection.AddTask(sceneTask);
            }
        }

        private List<IToCommand> toCommands { get; set; }

        private void SetToCommandList()
        {
            this.toCommands = new List<IToCommand>()
            {
                new SendFiles(this)
            };
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }
}
