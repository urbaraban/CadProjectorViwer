using CadProjectorSDK;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Config;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Scenes.Actions;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorViewer.Dialogs;
using CadProjectorViewer.EthernetServer;
using CadProjectorViewer.EthernetServer.Servers;
using CadProjectorViewer.Opening;
using CadProjectorViewer.Panels;
using CadProjectorViewer.Properties;
using CadProjectorViewer.Services;
using CadProjectorViewer.ToCommands;
using CadProjectorViewer.ToCommands.MainAppCommand;
using CadProjectorViewer.ViewModel.Modules;
using Microsoft.Win32;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;
using ToGeometryConverter;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.ViewModel
{
    public class AppMainModel : NotifyModel
    { 
        private Dispatcher Dispatcher { get; }

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

        [XmlElement(ElementName = "ProjectorHub")]
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

        public ToCutEthernetHub EthernetHub { get; } = new ToCutEthernetHub();

        private List<IToCommand> toCommands { get; } = new List<IToCommand>()
        {
            new FileListCommand(null, string.Empty),
        };

        public AppMainModel()
        {
            Dispatcher = Dispatcher.CurrentDispatcher;

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

            this.EthernetHub.CommandDummyIncomming += CUTServer_CommandDummyIncomming;
        }

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
                case MessageBoxResult.No:
                    ProgressPanel.End();
                    return false;
                case MessageBoxResult.Cancel:
                    ProgressPanel.End();
                    return true;
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

        public ICommand LoadMWSCommand => new ActionCommand(() => FileLoad.LoadMoncha(ProjectorHub, true));

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
                // FileSave.SaveScene(projectorHub.ScenesCollection.SelectedScene, saveFileDialog.FileName);
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
            ManipulatorTCPDialog manipulatorTCP = new ManipulatorTCPDialog()
            {
                DataContext = this
            };
            manipulatorTCP.Show();
        });

        public ICommand SaveConfig => new ActionCommand(() =>
        {
            var xmls = new XmlSerializer(this.GetType());
            var writer = new StreamWriter("D:\\Программы\\test.xml");
            xmls.Serialize(writer, this);
            writer.Close();
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
            if (await FileLoad.GetFilePath(path, this.ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value) is UidObject uidObject)
            {
                SceneTask sceneTask = new SceneTask()
                {
                    Object = uidObject,
                };
                await this.ProjectorHub.ScenesCollection.AddTask(sceneTask);
            }
        }
    }
}
