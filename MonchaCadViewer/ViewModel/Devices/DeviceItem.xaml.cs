using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Modules;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.DeviceManaged;
using CadProjectorViewer.Dialogs;
using Microsoft.Xaml.Behaviors.Core;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CadProjectorViewer.ViewModel.Devices
{
    /// <summary>
    /// Логика взаимодействия для DeviceItem.xaml
    /// </summary>
    public partial class DeviceItem : UserControl
    {
        public DeviceItem()
        {
            InitializeComponent();
        }

        public ICommand ShowRectCommand => new ActionCommand(() =>
        {
            if (this.DataContext is LProjector device)
            {

            }
        });

        public ICommand ReconnectCommand => new ActionCommand(async () => {
            if (this.DataContext is IConnected device)
            {
                await device.Reconnect();
            }
        });

        public ICommand ShowZoneRectCommand => new ActionCommand(() =>
        {
            if (this.DataContext is LProjector device && device.GetParentScene?.Invoke() is ProjectionScene Scene)
            {
                Scene.Add(device.Size);
            }
        });

        public ICommand ProjectorViewCommand => new ActionCommand(() =>
        {
            if (this.DataContext is IRenderingDisplay device)
            {
                ProjectorView projectorView = new ProjectorView(new RenderDeviceModel(device));
                projectorView.Show();
            }
        });

        public ICommand GoToWebInterface => new ActionCommand(() =>
        {
            if (this.DataContext is IConnected connected)
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

        public ICommand ShowModulesSettingCommand => new ActionCommand(() =>
        {
            if (this.DataContext is LProjector projector)
            {
                var dialog = new DeviceModulesDialog();
                var context = new AddDeviceModule(projector.ModulesGroup);
                dialog.DataContext = context;
                dialog.Show();
            }
        });

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            (sender as ContextMenu).DataContext = this;
        }
    }
}
