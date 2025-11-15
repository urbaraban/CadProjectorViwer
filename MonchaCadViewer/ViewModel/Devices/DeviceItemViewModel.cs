using CadProjectorSDK.Device;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.DeviceManaged;
using CadProjectorViewer.Dialogs;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace CadProjectorViewer.ViewModel.Devices
{
    public class DeviceItemViewModel(LProjector lProjector) : NotifyModel
    {
        private readonly LProjector lProjector = lProjector;

        public bool IsConnected => lProjector.IsConnected;
        public string IpAddress => (lProjector is IConnectebleDevice connectebleDevice) ? connectebleDevice.IpAddress : "N/A";
        public string DisplayName => lProjector.DisplayName;

        public ICommand ReconnectCommand => new RelayCommand(
            p => lProjector is IConnectebleDevice,
            async p => {
                if (lProjector is IConnectebleDevice connectebleDevice)
                {
                    await connectebleDevice.Reconnect();
                    OnPropertyChanged(nameof(IsConnected));
                }
            });

        public bool IsOn
        {
            get => lProjector.IsOn;
            set => lProjector.IsOn = value;
        }

        public ICommand ShowModulesSettingCommand => new RelayCommand(
            p => true,
            p => {
                var dialog = new DeviceModulesDialog();
                var context = new AddDeviceModule(lProjector);
                dialog.DataContext = context;
                dialog.Show();
            });


        public ICommand ShowZoneRectCommand => new RelayCommand(
            p => true,
            p =>
            {
                if (lProjector.GetParentScene?.Invoke() is ProjectionScene Scene)
                {
                    Scene.Add(lProjector.Size);
                }
            });

        public ICommand ProjectorViewCommand => new RelayCommand(
            p => true,
            p =>
            {
                if (this.lProjector is IRenderingDisplay device)
                {
                    ProjectorView projectorView = new ProjectorView(new RenderDeviceModel(device));
                    projectorView.Show();
                }
            });

        public ICommand GoToWebInterface => new RelayCommand(
            p => true,
            p =>
            {
                if (this.lProjector is IConnectebleDevice connected)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = $"http://{connected.IpAddress}",
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        MessageBox.Show("Не удалось открыть страницу", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                }
            });
    }
}
