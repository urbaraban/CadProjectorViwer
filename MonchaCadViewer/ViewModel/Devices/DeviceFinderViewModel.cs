using CadProjectorSDK;
using CadProjectorSDK.Device.Controllers;
using CadProjectorSDK.Tools;
using CadProjectorViewer.Dialogs.DeviceManaged;
using Microsoft.Xaml.Behaviors.Core;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CadProjectorViewer.ViewModel.Devices
{
    internal class DeviceFinderViewModel : NotifyModel
    {
        public ObservableCollection<IpDeviceInfo> OldDevices { get; } = new ObservableCollection<IpDeviceInfo>();
        public ObservableCollection<IpDeviceInfo> FindedDevices { get; } = new ObservableCollection<IpDeviceInfo>();

        private ProjectorSearcher ProjectorSearcher { get; } = new ProjectorSearcher();

        private ProjectorHub hub { get; }

        public DeviceFinderViewModel (ProjectorHub projectorHub)
        {
            this.hub = projectorHub;
            foreach(var item in projectorHub.Projectors)
            {
                OldDevices.Add(new IpDeviceInfo()
                {
                    iPAddress = item.IPAddress,
                    DvcType = item.DeviceType,
                    IsSelected = true
                });
            }
        }

        public ICommand OkCommand => new ActionCommand( async () =>
        {
            foreach (IpDeviceInfo device in FindedDevices)
            {
                if (device.IsSelected == true)
                {
                    hub.Projectors.Add(await DevicesMg.GetDeviceAsync(device.iPAddress, device.DvcType, hub.Projectors.Count));
                }
            }

            foreach (IpDeviceInfo device in OldDevices)
            {
                if (device.IsSelected == false)
                {
                    hub.Projectors.RemoveDevice(device.iPAddress);
                }
            }
        });

        public ICommand SearchCommand => new ActionCommand(() =>
        {
            FindedDevices.Clear();
            foreach(var item in ProjectorSearcher.GetMonchaBroadcastReplies())
            {
                FindedDevices.Add(item);
            }
        });

        public ICommand ManualAddCommand => new ActionCommand(() =>
        {
            var dialog = new AddDeviceByIpDialog();
            dialog.DataContext = new AddDeviceViewModel(this);
            dialog.Show();
        });
    }
}
