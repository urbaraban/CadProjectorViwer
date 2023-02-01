using CadProjectorSDK;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.CadObjects.Interfaces;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Render;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorViewer.Dialogs;
using CadProjectorViewer.EthernetServer;
using CadProjectorViewer.EthernetServer.Servers;
using CadProjectorViewer.StaticTools;
using CadProjectorViewer.ToCommands;
using CadProjectorViewer.ToCommands.MainAppCommand;
using CadProjectorViewer.ViewModel.Scenes.Actions;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.ViewModel.Scenes
{
    public class SceneModel : RenderDeviceModel, IToCutCommandObject
    {
        #region IToCutCommandObject
        public event EventHandler<ReceivedCookies> CommandDummyIncomming;

        public string Name => this.ProjectScene.DisplayName;
        #endregion

        public Selecting SelectedScene { get; set; }
        public delegate void Selecting(SceneModel Scene, bool stat);

        public RemovingDelegate Removed { get; set; }
        public delegate void RemovingDelegate(object Sender);

        private Dispatcher dispatcher { get; }
        public CadAnchor MousePosition => ProjectScene.MousePosition;


        public bool IsSelected
        {
            get => isselected;
            set
            {
                isselected = value;
                OnPropertyChanged("IsSelected");
                SelectedScene?.Invoke(this, isselected);
            }
        }
        private bool isselected;
        public bool ShowCursor
        {
            get => ProjectScene.ShowCursor;
            set
            {
                ProjectScene.ShowCursor = value;
                OnPropertyChanged("ShowCursor");
            }
        }
        public bool Play
        {
            get => this.ProjectScene.Play;
            set
            {
                ProjectScene.Play = value;
                OnPropertyChanged(nameof(Play));
            }
        }

        public double Width
        {
            get => ProjectScene.Size.Width;
            set
            {
                ProjectScene.Size.Width = value;
                OnPropertyChanged(nameof(Width));
            }
        }
        public double Height
        {
            get => ProjectScene.Size.Height;
            set
            {
                ProjectScene.Size.Height = value;
                OnPropertyChanged(nameof(Height));
            }
        }
        public double Depth
        {
            get => ProjectScene.Size.Depth;
            set
            {
                ProjectScene.Size.Depth = value;
                OnPropertyChanged(nameof(Depth));
            }
        }

        public double MoveSpeed
        {
            get => movespeed;
            set
            {
                movespeed = value;
                OnPropertyChanged(nameof(MoveSpeed));
            }
        }
        private double movespeed;

        public int TableID
        {
            get => this.ProjectScene.TableID;
            set
            {
                this.ProjectScene.TableID = value;
                OnPropertyChanged(nameof(TableID));
            }
        }

        public ComplexAction AlreadyAction
        {
            get => alreadyaction;
            set
            {
                if (alreadyaction != null)
                {
                    alreadyaction.EndEvent -= Alreadyaction_EndEvent;
                    alreadyaction.ReturnObj -= Action_ReturnObj;
                }
                alreadyaction = value;
                if (alreadyaction != null)
                {
                    alreadyaction.EndEvent += Alreadyaction_EndEvent;
                    alreadyaction.ReturnObj += Action_ReturnObj;
                }
                OnPropertyChanged(nameof(AlreadyAction));
            }
        }

        private void Alreadyaction_EndEvent()
        {
            this.AlreadyAction = (ComplexAction)this.AlreadyAction.Refresh;
            if (this.AlreadyAction is ComplexAction complexAction)
            {
                if (complexAction.ContinueAction == true)
                {
                    this.AlreadyAction.NextAction(this.MousePosition);
                }
            }
        }

        private ComplexAction alreadyaction;

        public void Action_ReturnObj(object Obj)
        {
            if (Obj is UidObject uidObject)
            {
                if (Obj is CadRect3D rect3d)
                {
                    rect3d.Multiply = this.ProjectScene.GetSize;
                    this.ProjectScene.AddMask(rect3d);
                }
                this.ProjectScene.Add(uidObject);
            }
        }

        public async Task Break()
        {
            if (this.AlreadyAction != null)
            {
                if (this.AlreadyAction.ActionObject is UidObject uidObject)
                    uidObject.Remove();
                this.AlreadyAction = null;
            }
        }

        public ObservableCollection<CadRect3D> Masks => ProjectScene.Masks;

        public ProjectionScene ProjectScene { get; set; }

        public override bool ShowHide => true;

        public override double Thinkess => Math.Max(Width, Height) * AppSt.Default.default_thinkess_percent;

        public SceneModel() : base(new ProjectionScene())
        {

        }

        public SceneModel(ProjectionScene scene) : base(scene)
        {
            this.dispatcher = Dispatcher.CurrentDispatcher;
            this.ProjectScene = scene;
        }

        private List<IToCommand> toCommands { get; } = new List<IToCommand>()
        {
            new FilePathCommand(null, string.Empty),
        };

        public IToCommand GetCommand(CommandDummy toCommand)
        {
            return this.toCommands.FirstOrDefault(e => e.Name == toCommand.Name);
        }

        public ICommand MaskCommand => new ActionCommand(() => {
            this.AlreadyAction = new DrawMaskAction(this.ProjectScene.Size);
        });

        public ICommand LineCommand => new ActionCommand(() => {
            this.AlreadyAction = new DrawLineAction();
        });

        public ICommand RefreshFrameCommand => new ActionCommand(() => {
            this.ProjectScene.RefreshScene();
        });
        public ICommand CancelActionCommand => new ActionCommand(() => {
            this.Break();
        });

        public bool PathLoad(string path)
        {
            bool result = File.Exists(path);
            if (result == true)
            {

                if (FileLoad.GetFilePath(path, this.ProjectScene.ProjectionSetting.PointStep.Value).Result is UidObject uidObject)
                {
                    uidObject.UpdateTransform(uidObject.Bounds, false, String.Empty);

                    SceneTask sceneTask = new SceneTask()
                    {
                        Object = uidObject,
                        TableID = this.TableID,
                    };

                    this.ProjectScene.RunTask(sceneTask, true);
                }

            }

            return result;
        }

        public void Clear() => this.ProjectScene.Clear();
    }
}
