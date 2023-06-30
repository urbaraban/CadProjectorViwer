using CadProjectorSDK.Device;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.Modeles;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace CadProjectorViewer.ViewModel
{
    internal class ScenesDeviceManagmentVM : ViewModelBase
    {
        public ProjectorCollection Projectors => AppMain.Projectors;
        public LProjector SelectedProjector { get; set; }

        public ScenesCollection Scenes => AppMain.Scenes;

        public ProjectionScene SelectedScene
        {
            get => AppMain.Scenes.SelectedScene;
            set
            {
                AppMain.Scenes.SelectedScene = value;
                OnPropertyChanged(nameof(SelectedScene));
            }
        }

        public AppMainModel AppMain { get; }

        public ScenesDeviceManagmentVM(AppMainModel model)
        {
            this.AppMain = model;
        }

        public ICommand MigrateDeviceCommand => new ActionCommand(MigrateDevice);
        private async void MigrateDevice()
        {
            if (this.SelectedProjector is LProjector device)
            {
                foreach (ProjectionScene Scene in Scenes)
                {
                    if (Scene != Scenes.SelectedScene)
                    {
                        Scene.RemoveDevice(device);
                    }
                }

                if (SelectedScene.Projectors.Contains(device) == false)
                {
                    SelectedScene.AddDevice(device);
                }
            }
        }

        public ICommand RemoveDeviceCommand => new ActionCommand(RemoveDevice);
        private async void RemoveDevice()
        {
            if (SelectedProjector is LProjector device)
            {
                SelectedScene.RemoveDevice(device);
            }
        }

        public ICommand RemoveSceneCommand => new ActionCommand(RemoveScene);
        private async void RemoveScene()
        {
            if (SelectedScene is ProjectionScene scene && AppMain.Scenes.Count > 1)
            {
                int index = Scenes.IndexOf(scene);
                SelectedScene = Scenes[Math.Abs(index - 1)];
                Scenes.Remove(scene);
            }
        }

        public ICommand AddSceneCommand => new ActionCommand(AddScene);
        private async void AddScene()
        {
            ProjectionScene newscene = new ProjectionScene();
            Scenes.Add(newscene);
            this.SelectedScene = newscene;
        }
    }


    public class ConvertModelToScenesDeviceManagment : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppMainModel model)
            {
                return new ScenesDeviceManagmentVM(model);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
