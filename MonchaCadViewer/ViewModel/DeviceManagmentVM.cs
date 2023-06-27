using CadProjectorSDK.Device;
using CadProjectorViewer.Dialogs;
using CadProjectorViewer.Modeles;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace CadProjectorViewer.ViewModel
{
    internal class DeviceManagmentVM : NotifyModel
    {
        public ProjectorCollection Projectors => AppMain.Projectors;

        public LProjector SelectProjector
        {
            get => AppMain.Projectors.SelectedItem;
            set
            {
                AppMain.Projectors.SelectedItem = value;
                OnPropertyChanged(nameof(SelectProjector));
            }
        }

        private AppMainModel AppMain { get; }
        internal DeviceManagmentVM(AppMainModel appMain)
        {
            this.AppMain = appMain;
        }

        public ICommand ShowSearchCommand => new ActionCommand(() => {
            LaserSearcher DeviceManaged = new LaserSearcher(AppMain);
            DeviceManaged.Show();
        });

        public ICommand RemoveCommand => new ActionCommand(() => {
            if (SelectProjector != null)
            {
                SelectProjector.Remove();
            }
        });

        public ICommand ShowSceneManagerCommand => new ActionCommand(() => {
            SceneSettingWindow sceneSettingWindow = new SceneSettingWindow();
            sceneSettingWindow.DataContext = new ScenesDeviceManagmentVM(AppMain);
            sceneSettingWindow.Show();
        });
    }

    public class ConvertModelToDeviceManagment : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppMainModel model)
            {
                return new DeviceManagmentVM(model);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
