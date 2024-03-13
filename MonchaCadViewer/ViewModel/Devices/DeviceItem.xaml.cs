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

        public ICommand PolyMeshCommand => new ActionCommand(async () => {
            if (this.DataContext is LProjector device)
            {
                device.PolyMeshUsed = !device.PolyMeshUsed;
            }
        });

        public ICommand ShowZoneRectCommand => new ActionCommand(() =>
        {
            if (this.DataContext is LProjector device && device.GetParentScene?.Invoke() is ProjectionScene Scene)
            {
                Scene.Add(device.Size);
            }
        });

        public ICommand ShowCenterCommand => new ActionCommand(() =>
        {
            if (this.DataContext is LProjector device && device.GetParentScene?.Invoke() is ProjectionScene Scene)
            {
                Scene.Add(new CadAnchor(device.Center)
                {
                    Multiply = Scene.GetSize
                });
            }
        });

        public ICommand ProjectorViewCommand => new ActionCommand(() =>
        {
            if (this.DataContext is IRenderingDisplay device)
            {
                ProjectorView projectorView = new ProjectorView()
                {
                    DataContext = new RenderDeviceModel(device)
                };
                projectorView.Show();
            }
        });

        public ICommand GoToWebInterface => new ActionCommand(() =>
        {
            if (this.DataContext is IConnected connected)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"http://{connected.IpAddress}"
                });
            }
        });

        public ICommand ShowModulesSettingCommand => new ActionCommand(() =>
        {
            if (this.DataContext is ModulesGroup group)
            {
                var dialog = new DeviceModulesDialog();
                var context = new AddDeviceModule(group);
                dialog.DataContext = context;
                dialog.ShowDialog();
            }
        });

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            (sender as ContextMenu).DataContext = this;
        }
    }
}
