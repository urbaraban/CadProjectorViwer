using CadProjectorSDK.Device;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.DeviceManaged;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

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
                    FileName = $"http://{connected.IPAddress.ToString()}"
                });
            }
        });

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            (sender as ContextMenu).DataContext = this;
        }
    }
}
