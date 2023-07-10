using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Device;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Scenes.Actions;
using CadProjectorViewer.EthernetServer.Servers;
using CadProjectorViewer.Opening;
using CadProjectorViewer.ToCommands;
using CadProjectorViewer.ToCommands.MainAppCommand;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.ViewModel.Scene
{
    internal class SceneModel : RenderDeviceModel, IToCutCommandObject
    {
        #region IToCutCommandObject
        public event EventHandler<ReceivedCookies> CommandDummyIncomming;

        public string Name => this.Scene.DisplayName;
        #endregion

        public ComplexAction AlreadyAction
        {
            get => this.Scene.AlreadyAction;
            set
            {
                this.Scene.AlreadyAction = value;
                OnPropertyChanged(nameof(AlreadyAction));
            }
        }

        private Dispatcher dispatcher { get; }
        public CadAnchor MousePosition => Scene.MousePosition;

        public bool ShowCursor
        {
            get => Scene.ShowCursor;
            set
            {
                Scene.ShowCursor = value;
                OnPropertyChanged("ShowCursor");
            }
        }

        public ObservableCollection<CadRect3D> Masks => Scene.Masks;

        public ObservableCollection<LProjector> Projectors => Scene.Projectors;

        public ProjectionScene Scene { get; set; }

        public int TableID => this.Scene.TableID;

        public override bool ShowHide => true;
        public override double Width { get; set; } = 3000;
        public override double Height { get; set; } = 3000;
        public override double Thinkess => Math.Max(Width, Height) * AppSt.Default.default_thinkess_percent;

        public SceneModel(ProjectionScene scene) : base(scene)
        {
            this.dispatcher = Dispatcher.CurrentDispatcher;
            this.Scene = scene;
        }

        public ICommand RefreshFrameCommand => new ActionCommand(() => {
            Scene.RefreshScene();
        });
        public ICommand CancelActionCommand => new ActionCommand(() => {
            Scene.Break();
        });

        private List<IToCommand> toCommands { get; } = new List<IToCommand>()
        {
            new FilePathCommand(null, string.Empty),
        };

        public IToCommand GetCommand(CommandDummy toCommand)
        {
            return this.toCommands.FirstOrDefault(e => e.Name == toCommand.Name);
        }

        public bool PathLoad(string path)
        {
            bool result = File.Exists(path);
            if (result == true)
            {

                if (FileLoad.GetFilePath(path).Result is UidObject uidObject)
                {
                    uidObject.UpdateTransform(uidObject.Bounds, false, String.Empty);

                    SceneTask sceneTask = new SceneTask()
                    {
                        Object = uidObject,
                        TableID = this.TableID,
                    };

                    this.Scene.RunTask(sceneTask, true);
                }

            }

            return result;
        }
    }
}
