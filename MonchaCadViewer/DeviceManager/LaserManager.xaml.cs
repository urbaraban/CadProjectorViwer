using MonchaNETDll.MonchaBroadcast;
using MonchaSDK;
using MonchaSDK.Device;
using System.Collections.Generic;
using System.Net;
using System.Windows;


namespace MonchaCadViewer
{
    /// <summary>
    /// Логика взаимодействия для DeviceManager.xaml
    /// </summary>
    public partial class LaserManager : Window
    {
        private List<IPAddress> iPs = new List<IPAddress>();
        public List<MonchaDevice> Devices = new List<MonchaDevice>();

        public LaserManager()
        {
            InitializeComponent();
            RefreshList();

        }

        private void RefreshList()
        {
            this.iPs = MonchaSearch.FindDevicesOverBroadcast(MonchaSearch.GetAvailabeBroadcastAddresses());

            if (this.iPs.Count > 0)
            {
                this.Devices.Clear();
                for (int i = 0; i < iPs.Count; i++) //no include work laser (change to i = 0)
                {
                    //if not
                    if (!MonchaHub.CheckDeviceIP(iPs[i]))
                    {
                        MonchaDevice device = MonchaHub.ConnectDevice(iPs[i]);

                        if (device != null)
                            this.Devices.Add(device);
                    }
                }
            }

            if (this.Devices.Count > 0)
            {
                DeviceList.ItemsSource = this.Devices;
            }

        }

        private void DeviceManagerForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (MonchaDevice device in DeviceList.Items)
            {
                if (device != null && device.Selected)
                    MonchaHub.Devices.Add(device);
                else
                {
                    device.Disconnect();
                }
            }

            MonchaHub.RefreshDevice();
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
