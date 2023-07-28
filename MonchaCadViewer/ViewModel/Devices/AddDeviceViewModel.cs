using CadProjectorSDK;
using CadProjectorSDK.Device.Controllers;
using CadProjectorSDK.Tools;
using Microsoft.Xaml.Behaviors.Core;
using System.Windows.Input;

namespace CadProjectorViewer.ViewModel.Devices
{
    internal class AddDeviceViewModel
    {
        private DeviceFinderViewModel mainModel { get; }

        public byte ip_1 { get; set; } = 127;
        public byte ip_2 { get; set; } = 0;
        public byte ip_3 { get; set; } = 0;
        public byte ip_4 { get; set; } = 1;

        public ushort Port { get; set; } = 5011;

        public DeviceType SelectType { get; set; } = DeviceType.Virtual;

        public DeviceType[] DeviceTypes => DevicesMg.deviceTypes;

        public AddDeviceViewModel(DeviceFinderViewModel model)
        {
            this.mainModel = model;
        }

        public ICommand AddDeviceCommand => new ActionCommand( async () => {
            var projector = new IpDeviceInfo()
            {
                iPAddress = new System.Net.IPAddress(new byte[]
                {
                    this.ip_1,
                    this.ip_2,
                    this.ip_3,
                    this.ip_4
                }),
                DvcType = SelectType,
                IsSelected = true
            };

            this.mainModel.FindedDevices.Add(projector);
        });

    }
}
