using CadProjectorSDK;
using CadProjectorSDK.Device;
using CadProjectorSDK.Scenes;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CadProjectorViewer.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для SceneSettingWindow.xaml
    /// </summary>
    public partial class SceneSettingWindow : Window
    {
        private ProjectorHub Hub => (ProjectorHub)this.DataContext;

        public SceneSettingWindow()
        {
            InitializeComponent();
        }

        public ICommand MigrateDeviceCommand => new ActionCommand(MigrateDevice);
        private async void MigrateDevice()
        {
            if (Hub.SelectDevice is LDevice device)
            {
                foreach(ProjectionScene Scene in this.Hub.ScenesCollection)
                {
                    if (Scene != this.Hub.ScenesCollection.SelectedScene)
                    {
                        Scene.RemoveDevice(device);
                    }
                }

                if (this.Hub.ScenesCollection.SelectedScene.Devices.Contains(device) == false)
                {
                    this.Hub.ScenesCollection.SelectedScene.AddDevice(device);
                }
            }
        }

        public ICommand RemoveDeviceCommand => new ActionCommand(RemoveDevice);
        private async void RemoveDevice()
        {
            if (this.Hub.ScenesCollection.SelectedScene.SelectDevice is LDevice device)
            {
                this.Hub.ScenesCollection.SelectedScene.RemoveDevice(device);
            }
        }

        public ICommand RemoveSceneCommand => new ActionCommand(RemoveScene);
        private async void RemoveScene()
        {
            if (this.Hub.ScenesCollection.SelectedScene is ProjectionScene scene && this.Hub.ScenesCollection.Count > 1)
            {
                int index = this.Hub.ScenesCollection.IndexOf(scene);
                this.Hub.ScenesCollection.SelectedScene = this.Hub.ScenesCollection[Math.Abs(index - 1)];
                this.Hub.ScenesCollection.Remove(scene);
            }
        }

        public ICommand AddSceneCommand => new ActionCommand(AddScene);
        private async void AddScene()
        {
            this.Hub.ScenesCollection.Add(new ProjectionScene());
        }
    }
}
