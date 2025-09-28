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
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
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
        public override double Width => Size.Width;
        public override double Height => Size.Height;
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

        public ICommand CentertAttachCommand => new ActionCommand(() =>
        {
            this.Scene.DefAttach = this.GetNewAttach("middle%middle");
            AttachObjects(this.Scene.DefAttach);
        });

        public ICommand LeftAttachCommand => new ActionCommand(() =>
        {   
            this.Scene.DefAttach = this.GetNewAttach("empty%left");
            AttachObjects(this.Scene.DefAttach);
        });

        public ICommand RightAttachCommand => new ActionCommand(() =>
        {
            this.Scene.DefAttach = this.GetNewAttach("empty%right");
            AttachObjects(this.Scene.DefAttach);
        });


        public ICommand TopAttachCommand => new ActionCommand(() =>
        {
            this.Scene.DefAttach = this.GetNewAttach("top%empty");
            AttachObjects(this.Scene.DefAttach);
        });

        public ICommand DownAttachCommand => new ActionCommand(() =>
        {
            this.Scene.DefAttach = this.GetNewAttach("down%empty");
            AttachObjects(this.Scene.DefAttach);
        });

        public ICommand RotateCommand => new ActionCommand(() => {
            IEnumerable<UidObject> objects = GetSelectOrNotObjects();
            foreach (UidObject obj in objects)
            {
                obj.AngleZ += 90;
            }
            AttachObjects(this.Scene.DefAttach);
        });

        private void AttachObjects(string new_attach)
        {
            IEnumerable<UidObject> objects = GetSelectOrNotObjects();
            
            if (objects.Any() == true)
            {
                var commonBounds = objects.ElementAt(0).Bounds;
                foreach (UidObject obj in objects)
                {
                    commonBounds.Union(obj.Bounds);
                }

                var delta = TransformObject.DeltaToEdge(
                    commonBounds,
                    this.Size.Bounds,
                    this.Scene.DefAttach,
                    this.Scene.AttachDistanceX,
                    this.Scene.AttachDistanceY);

                foreach (UidObject obj in objects)
                {
                    obj.MX += delta.dX;
                    obj.MY += delta.dY;
                }

            }
        }

        private string GetNewAttach(string NewAttach)
        {
            if (string.IsNullOrEmpty(NewAttach) == false)
            {
                string[] new_strings = NewAttach.Split('%');
                string[] old_strings = this.Scene.DefAttach.Split('%');
                for (int i = 0; i < new_strings.Length; i += 1)
                {
                    if (new_strings[i].ToLower() != "empty")
                    {
                        old_strings[i] = new_strings[i];
                    }
                }
                return string.Join("%", old_strings);
            }
            return string.Empty;
        }

        private IEnumerable<UidObject> GetSelectOrNotObjects()
        {
            ObservableCollection<UidObject> objects = this.Scene.SelectedObjects;
            if (objects.Count == 0)
            {
                objects = this.Scene;
            }
            return objects;
        }

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

                if (FileLoad.GetFilePath(path, this.Scene.ProjectionSetting.PointStep.Value).Result is UidObject uidObject)
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
