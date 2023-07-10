using System.Collections.Generic;
using System.Linq;
using AppSt = CadProjectorViewer.Properties.Settings;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using Microsoft.Xaml.Behaviors.Core;
using System.Windows;
using System.Windows.Input;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorSDK.Scenes.Actions;
using System.IO;
using Microsoft.Win32;
using ToGeometryConverter;
using CadProjectorViewer.ViewModel.Modules;
using System.Diagnostics;
using CadProjectorViewer.EthernetServer;
using CadProjectorViewer.Dialogs;
using CadProjectorViewer.ToCommands;
using CadProjectorViewer.ToCommands.MainAppCommand;
using System.Windows.Threading;
using CadProjectorViewer.EthernetServer.Servers;
using CadProjectorViewer.Opening;
using CadProjectorSDK.Device;
using CadProjectorSDK.Interfaces;
using System.Net;
using CadProjectorSDK.Tools;
using CadProjectorViewer.Services;
using CadProjectorSDK.Device.Controllers;
using CadProjectorViewer.Configurations;
using CadProjectorViewer.ViewModel;
using CadProjectorSDK.Services;
using LogList = CadProjectorViewer.Services.LogList;

namespace CadProjectorViewer.Modeles
{
    public class AppMainModel : ViewModelBase
    { 
        private Dispatcher dispatcher { get; }

        public bool AdminMode => Debugger.IsAttached == true || Adminclick > 9;
        public int Adminclick 
        {
            get => _adminclick;
            set
            {
                _adminclick = value;
                OnPropertyChanged(nameof(AdminMode));
            }
        }
        private int _adminclick = 0;

        public LockKey LockKey { get; } = LockKey.Instance;

        public ProjectorCollection Projectors { get; } = new ProjectorCollection()
        {
            new VirtualProjector()
        };

        public ScenesCollection Scenes { get; } = new ScenesCollection() {
            new ProjectionScene() 
        };

        public TaskCollection Tasks { get; } = TaskCollection.Instance;

        public WorkFolderList WorkFolder { get; } = new WorkFolderList();

        public ToCutEthernetHub EthernetHub { get; } = new ToCutEthernetHub();

        private List<IToCommand> toCommands { get; } = new List<IToCommand>()
        {
            new FileListCommand(null, string.Empty),
        };

        public AppMainModel()
        {
            LogList.Instance.PostLog("Start working", "App");

            dispatcher = Dispatcher.CurrentDispatcher;

            CadProjectorSDK.Services.LogList.Instance.Post += LogList.Instance.PostLog;

            GCTools.Log = LogList.Instance.PostLog;
            GCTools.SetProgress = Progress.Instance.SetProgress;

            WorkFolder.PathSelected += WorkFolder_PathSelected;
            Tasks.SelectedTask += Tasks_SelectedTask;

            this.EthernetHub.CommandDummyIncomming += CUTServer_CommandDummyIncomming;
        }

        private void Tasks_SelectedTask(object sender, SceneTask e) => Scenes.RunTask(e);

        private void CUTServer_CommandDummyIncomming(object sender, ReceivedCookies e)
        {
            if (sender is ToCutServerObject toCutServerObject)
            {
                foreach (var command in e.Dummies)
                {
                    if (ToCommand.GetToCommand(command.Name, this.toCommands) is IToCommand toCommand)
                    {
                        IToCommand exCommand = toCommand.MakeThisCommand(this, command.Message);
                        ExecutCommand(exCommand, toCutServerObject, e);
                    }
                }
            }
        }

        private void ExecutCommand(IToCommand toCommand, ToCutServerObject toCutServerObject, ReceivedCookies cookies)
        {
            object result = toCommand.Run();

            if (toCommand.ReturnRequest == true && result is string message)
            {
                toCutServerObject.SendMessage(message, cookies);
            }
            else if (result is IToCommand newcommand)
            {
                ExecutCommand(newcommand, toCutServerObject, cookies);
            }
        }

        private async void WorkFolder_PathSelected(object sender, string e) => OpenGeometryFile(e);

        public ICommand SaveCommand => new ActionCommand(() => SaveConfiguration(false));

        public ICommand SaveAsCommand => new ActionCommand(() => SaveConfiguration(true));

        public bool CheckDeviceInHub(IPAddress iPAddress)
        {
            foreach (LProjector device in Projectors)
            {
                if (device is IConnected connected)
                {
                    if (connected.IPAddress.Address.Equals(iPAddress.Address))
                    {
                        return true;
                    }
                }
            }

            //foreach (VLTLaserMeters laser in LMeters)
            //    if (laser != null)
            //    {
            //        if (laser.IP.Address.Equals(iPAddress.Address))
            //        {
            //            return true;
            //        }
            //    }

            return false;
        }

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
                            Progress.Instance.SetProgress(1, 2, "Save Moncha");
                            mws.Save(saveFileDialog.FileName, this);
                            if (File.Exists(saveFileDialog.FileName) == false)
                            {
                                AppSt.Default.cl_moncha_path = saveFileDialog.FileName;
                                Progress.Instance.SetProgress(2, 2, "Not Save");
                                SaveConfiguration(true);
                            }
                            else
                            {
                                Progress.Instance.SetProgress(2, 2, "Saved");
                                AppSt.Default.cl_moncha_path = saveFileDialog.FileName;
                            }
                        }
                    }
                    else
                    {
                        mws.Save(AppSt.Default.cl_moncha_path, this);
                    }
                    AppSt.Default.Save();
                    Progress.Instance.End();
                    return false;
                    break;
                case MessageBoxResult.No:
                    Progress.Instance.End();
                    return false;
                    break;
                case MessageBoxResult.Cancel:
                    Progress.Instance.End();
                    return true;
                    break;

            }
            Progress.Instance.SetProgress(2, 2, "Save Setting");

            Progress.Instance.End();
            return false;
        }

        public ICommand MaskCommand => new ActionCommand(() => {
            Scenes.SelectedScene.AlreadyAction = new DrawMaskAction(Scenes.SelectedScene.Size);
        });

        public ICommand LineCommand => new ActionCommand(() => {
            Scenes.SelectedScene.AlreadyAction = new DrawLineAction();
        });


        public ICommand Clear => new ActionCommand(() => {
            Scenes.SelectedScene.Clear();
        });

        public ICommand SelectNextCommand => new ActionCommand(() => {
            Scenes.SelectedScene.HistoryCommands.Add(
                        new SelectNextCommand(true, Scenes.SelectedScene));
        });

        public ICommand SelectPreviousCommand => new ActionCommand(() => {
            Scenes.SelectedScene.HistoryCommands.Add(
                        new SelectNextCommand(false, Scenes.SelectedScene));
        });

        public ICommand DeleteCommand => new ActionCommand(() => {
            Scenes.SelectedScene.RemoveRange(Scenes.SelectedScene.SelectedObjects);
        });

        public ICommand UndoCommand => new ActionCommand(() => {
            Scenes.SelectedScene.HistoryCommands.UndoLast();
        });

        public ICommand ShowLicenceCommand => new ActionCommand(() => {
            RequestLicenseCode requestLicenseCode = new RequestLicenseCode() { DataContext = LockKey };
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
                    TableID = this.Scenes.SelectedScene.TableID,
                    Object = await FileLoad.GetCliboard()
                };
                this.Tasks.Add(sceneTask);
            }
            catch
            {
                LogList.Instance.PostLog("Clipboard is not geometry", "APP");
            }
        }

        public ICommand PlayCommand => new ActionCommand(() => {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                this.Scenes.SelectedScene.Play = !this.Scenes.SelectedScene.Play;
            }
            else
            {
                PlayAllCommand.Execute(null);
            }
        });

        public ICommand PlayAllCommand => new ActionCommand(() =>
        {
            bool stat = !this.Scenes.Any(sc => sc.Play);
            foreach (ProjectionScene scene in this.Scenes)
            {
                scene.Play = stat;
            }
        });

        public ICommand SaveSceneCommand => new ActionCommand(() => {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "2CUT Scene (*.2scn)|*.2scn";
            if (saveFileDialog.ShowDialog() == true)
            {
                FileSave.SaveScene(Scenes.SelectedScene, saveFileDialog.FileName);
                //SaveScene.WriteXML(projectorHub.ScenesCollection.SelectedScene, saveFileDialog.FileName);
            }

        });

        //public ICommand OpenSceneCommand => new ActionCommand(() => {
        //    OpenFileDialog fileDialog = new OpenFileDialog();
        //    fileDialog.Filter = "Moncha (.2scn)|*.2scn|All Files (*.*)|*.*";
        //    if (fileDialog.ShowDialog() == true)
        //    {
        //        Tasks.AddTask(new SceneTask(SaveScene.ReadXML(fileDialog.FileName)));
        //    }
        //});

        public ICommand ShowTCPDialogCommand => new ActionCommand(() => {
            ManipulatorTCPDialog manipulatorTCP = new ManipulatorTCPDialog()
            {
                DataContext = this
            };
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

        public async void OpenGeometryFile(string path)
        {
            if (await FileLoad.GetFilePath(path) is UidObject uidObject)
            {
                SceneTask sceneTask = new SceneTask()
                {
                    Object = uidObject,
                };
                await this.Tasks.AddTask(sceneTask);
            }
        }

        public void Disconnect()
        {
            foreach (LProjector device in Projectors)
            {
                if (device is LProjector projector)
                {
                    projector.Disconnect();
                }
            }
        }
    }
}
