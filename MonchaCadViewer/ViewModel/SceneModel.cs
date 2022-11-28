using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Interfaces;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Render;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Scenes.Actions;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorViewer.Dialogs;
using CadProjectorViewer.EthernetServer;
using CadProjectorViewer.EthernetServer.Servers;
using CadProjectorViewer.ToCommands;
using CadProjectorViewer.ToCommands.MainAppCommand;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.ViewModel
{
    public class SceneModel : RenderDeviceModel, IToCutCommandObject
    {
        #region IToCutCommandObject
        public event EventHandler<ReceivedCookies> CommandDummyIncomming;

        public string Name => this.Scene.DisplayName;
        #endregion

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

        public ProjectionScene Scene { get; set; }

        public override bool ShowHide => true;
        public override double Width => Size.Width;
        public override double Height => Size.Height;
        public override double Thinkess => Math.Max(Width, Height) * AppSt.Default.default_thinkess_percent;

        public SceneModel(ProjectionScene scene) : base(scene)
        {
            this.dispatcher = Dispatcher.CurrentDispatcher;
            this.Scene = scene;
        }

        private List<IToCommand> toCommands { get; } = new List<IToCommand>()
        {
                new FilePathCommand(null, string.Empty),
        };

        public IToCommand GetCommand(CommandDummy toCommand)
        {
            return this.toCommands.FirstOrDefault(e => e.Name == toCommand.Name);
        }
    }
}
