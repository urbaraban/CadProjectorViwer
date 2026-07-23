using CadProjectorSDK;
using CadProjectorSDK.CadObjects;
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
    [XmlRoot]
    public class AppMainModel : NotifyModel
    {
        [XmlElement]
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


        public ToCutEthernetHub EthernetHub { get; } = new ToCutEthernetHub();

        [XmlIgnore]
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
        public bool AdminMode => Debugger.IsAttached == true || Adminclick > 9;

        [XmlIgnore]
        public LogList Logs { get; } = new LogList(string.Empty);

        [XmlIgnore]
        public WorkFolderList WorkFolder { get; } = new WorkFolderList();

        private Dispatcher Dispatcher { get; }

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
            GCTools.DxfUnitsOverride = NormalizeDxfOverride(AppSt.Default.dxf_units_override);

            projectorHub.UDPLaserListener.OutFilePathWorker = FileLoad.GetUDPString;

            if (AppSt.Default.udp_auto_run == true)
            {
                projectorHub.UDPLaserListener.Run(AppSt.Default.ether_udp_port);
            }

            WorkFolder.PathSelected += WorkFolder_PathSelected;

            this.EthernetHub.CommandDummyIncomming += CUTServer_CommandDummyIncomming;
        }

        private static string NormalizeDxfOverride(string value)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "mm":
                case "cm":
                case "m":
                case "in":
                case "ft":
                    return value.Trim().ToLowerInvariant();
                default:
                    return "auto";
            }
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

        private void WorkFolder_PathSelected(object sender, string e) => OpenGeometryFile(e);

        public ICommand SaveCommand => new ActionCommand(() => SaveConfiguration(false));

        public ICommand SaveAsCommand => new ActionCommand(() => SaveConfiguration(true));

        private bool SaveConfiguration(bool saveas)
        {
            bool SaveToPath(string path)
            {
                try
                {
                    bool result = ProjectorHub.Save(path).GetAwaiter().GetResult();

                    if (string.IsNullOrWhiteSpace(Mws.LastSaveMessage) == false)
                    {
                        MessageBox.Show(
                            Mws.LastSaveMessage,
                            result ? "Предупреждение сохранения" : "Ошибка сохранения",
                            MessageBoxButton.OK,
                            result ? MessageBoxImage.Warning : MessageBoxImage.Error);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    App.Log?.Invoke($"Save error: {ex.Message}", "APP");
                    MessageBox.Show(
                        $"Сохранение завершилось ошибкой:\n{ex.Message}",
                        "Ошибка сохранения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }
            }

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
                            bool saved = SaveToPath(saveFileDialog.FileName);
                            if (saved == false)
                            {
                                ProgressPanel.SetProgressBar(2, 2, "Not Save");
                                return true;
                            }

                            if (File.Exists(saveFileDialog.FileName))
                            {
                                ProgressPanel.SetProgressBar(2, 2, "Saved");
                                AppSt.Default.cl_moncha_path = saveFileDialog.FileName;
                            }
                            else
                            {
                                ProgressPanel.SetProgressBar(2, 2, "Saved (recovery)");
                            }
                        }
                    }
                    else
                    {
                        bool saved = SaveToPath(AppSt.Default.cl_moncha_path);
                        ProgressPanel.SetProgressBar(2, 2, saved ? "Saved" : "Not Save");
                        if (saved == false)
                        {
                            return true;
                        }
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
                new SelectNextCommand(true, ProjectorHub.ScenesCollection.SelectedScene, false));
        });

        public ICommand SelectPreviousCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.Add(
                new SelectNextCommand(false, ProjectorHub.ScenesCollection.SelectedScene, false));
        });

        public ICommand DeleteCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.RemoveRange(ProjectorHub.ScenesCollection.SelectedScene.SelectedObjects);
        });

        public ICommand UndoCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.UndoLast();
        });

        public ICommand RedoCommand => new ActionCommand(() => {
            ProjectorHub.ScenesCollection.SelectedScene.HistoryCommands.RedoLast();
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
            if (projectorHub.ScenesCollection.SelectedScene == null)
            {
                MessageBox.Show(
                    "Нет выбранной сцены для сохранения.",
                    "Сохранить сцену",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "2CUT Scene (*.2scn)|*.2scn";
            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                IReadOnlyList<string> warnings = SaveScene.WriteXML(
                    projectorHub.ScenesCollection.SelectedScene,
                    saveFileDialog.FileName);

                if (warnings.Count > 0)
                {
                    MessageBox.Show(
                        "Сцена сохранена с предупреждениями:" + Environment.NewLine +
                        string.Join(Environment.NewLine, warnings),
                        "Сохранить сцену",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка сохранения сцены",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        });

        public ICommand OpenSceneCommand => new ActionCommand(async () => {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "2CUT Scene (*.2scn)|*.2scn|All Files (*.*)|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                try
                {
                    SceneLoadResult result = await SaveScene.LoadSceneAsync(
                        fileDialog.FileName,
                        devices: null,
                        async path => await FileLoad.GetFilePath(path, 0));

                    if (result.IsLegacyObjectsRoot)
                    {
                        UidObject? legacy = await FileLoad.LoadLegacy2CutAsync(fileDialog.FileName);
                        if (legacy != null)
                        {
                            await projectorHub.ScenesCollection.AddTask(new SceneTask(legacy));
                        }
                        else
                        {
                            MessageBox.Show(
                                "Не удалось загрузить устаревший формат Objects.",
                                "Открыть сцену",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                        return;
                    }

                    ProjectionScene target = projectorHub.ScenesCollection.SelectedScene
                        ?? projectorHub.ScenesCollection.FirstOrDefault();
                    if (target == null)
                    {
                        projectorHub.ScenesCollection.Add(result.Scene);
                        result.Scene.IsSelected = true;
                    }
                    else
                    {
                        ApplyLoadedSceneToTarget(target, result.Scene);
                    }

                    if (result.Warnings.Count > 0)
                    {
                        MessageBox.Show(
                            string.Join(Environment.NewLine, result.Warnings),
                            "Открыть сцену — предупреждения",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Ошибка открытия сцены",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
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

        private void Open()
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
                    TaskName = Path.GetFileName(path)
                };
                await this.ProjectorHub.ScenesCollection.AddTask(sceneTask);
            }
        }

        /// <summary>
        /// Merges a loaded scene into the active workplace scene without rebinding projectors
        /// (AddDevice on a temp scene would steal StartRenderDevice/RenderObjects and break render).
        /// </summary>
        private static void ApplyLoadedSceneToTarget(ProjectionScene target, ProjectionScene loaded)
        {
            target.NameID = loaded.NameID;
            target.DefAttach = loaded.DefAttach;
            target.AttachDistanceX = loaded.AttachDistanceX;
            target.AttachDistanceY = loaded.AttachDistanceY;
            target.CursorMaskActivated = loaded.CursorMaskActivated;
            target.StepByStep = loaded.StepByStep;
            target.IsNotUsedLayers = loaded.IsNotUsedLayers;
            target.ChangeFlags = loaded.ChangeFlags;
            target.Flags = loaded.Flags;
            target.DefaultAngle = loaded.DefaultAngle;
            target.DefaultMirror = loaded.DefaultMirror;
            target.DefaultScaleX = loaded.DefaultScaleX;
            target.DefaultScaleY = loaded.DefaultScaleY;

            // Keep workplace Size/ProjectionSetting instances (Multiply/UI bind to them).
            // Only adopt loaded Size when target has none.
            if (target.Size == null && loaded.Size != null)
            {
                target.Size = loaded.Size;
            }
            if (target.ProjectionSetting == null && loaded.ProjectionSetting != null)
            {
                target.ProjectionSetting = loaded.ProjectionSetting;
            }
            else if (loaded.ProjectionSetting?.PointStep != null && target.ProjectionSetting != null)
            {
                target.ProjectionSetting.PointStep = loaded.ProjectionSetting.PointStep;
                target.ProjectionSetting.Red = loaded.ProjectionSetting.Red;
                target.ProjectionSetting.Green = loaded.ProjectionSetting.Green;
                target.ProjectionSetting.Blue = loaded.ProjectionSetting.Blue;
                target.ProjectionSetting.RedOn = loaded.ProjectionSetting.RedOn;
                target.ProjectionSetting.GreenOn = loaded.ProjectionSetting.GreenOn;
                target.ProjectionSetting.BlueOn = loaded.ProjectionSetting.BlueOn;
            }

            target.Masks.Clear();
            foreach (CadRect3D mask in loaded.Masks.ToList())
            {
                loaded.Masks.Remove(mask);
                target.AddMask(mask);
            }

            target.SuppressInsertRender = true;
            try
            {
                target.Clear();
                foreach (UidObject obj in loaded.ToList())
                {
                    loaded.Remove(obj);
                    target.Add(obj);
                }
            }
            finally
            {
                target.SuppressInsertRender = false;
            }

            target.RefreshScene();
        }
    }
}
