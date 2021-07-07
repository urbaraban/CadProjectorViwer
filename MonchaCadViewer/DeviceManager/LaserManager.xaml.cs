using MonchaNETDll.MonchaBroadcast;
using MonchaSDK;
using MonchaSDK.Device;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Windows;


namespace MonchaCadViewer
{
    /// <summary>
    /// Логика взаимодействия для DeviceManager.xaml
    /// </summary>
    public partial class LaserManager : Window
    {
        private List<BroadcastReply2> iPs = new List<BroadcastReply2>();
        public List<IpSelect> OldDevices = new List<IpSelect>();
        public List<IpSelect> NewDevices = new List<IpSelect>();

        public LaserManager()
        {
            InitializeComponent();
            RefreshList();
        }

        private void RefreshList()
        {
            this.iPs = MonchaSearch.FindDevicesOverBroadcast2(MonchaSearch.GetAvailabeBroadcastAddresses());

            if (this.iPs.Count > 0)
            {
                this.NewDevices.Clear();
                foreach(BroadcastReply2 broadcastReply in iPs)
                {
                    IpSelect ipSelect = new IpSelect() { iPAddress = new IPAddress(BitConverter.GetBytes(broadcastReply.ipv4)), IsSelected = false };
                    //if not
                    if (MonchaHub.CheckDeviceInHub(ipSelect.iPAddress) == false)
                    {
                        this.NewDevices.Add(ipSelect);
                    }
                }
            }

            this.OldDevices.Clear();
            foreach (MonchaDevice monchaDevice in MonchaHub.Devices)
            {
                if (monchaDevice != null)
                {
                    this.OldDevices.Add(new IpSelect() { iPAddress = monchaDevice.iPAddress, IsSelected = true });
                }
            }

            if (this.NewDevices.Count > 0)
            {
                FoundDeviceList.ItemsSource = this.NewDevices;
                MonchaDeviceList.ItemsSource = this.OldDevices;
            }

        }

        private void DeviceManagerForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (IpSelect device in FoundDeviceList.Items)
            {
                if (device != null && device.IsSelected == true)
                {
                    MonchaHub.Devices.Add(MonchaHub.ConnectDevice(device.iPAddress));
                }
            }

            foreach (IpSelect device in MonchaDeviceList.Items)
            {
                if (device != null && device.IsSelected == false)
                {
                    MonchaHub.RemoveDevice(device.iPAddress);
                }
            }
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddVirtualBtn_Click(object sender, RoutedEventArgs e)
        {
            MonchaHub.Devices.Add(new MonchaDevice(new IPAddress(new byte[] { 127, 0, 0, 1 })));
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }
    }

    public class IpSelect : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private IPAddress ipadress;
        private bool selected;
        public IPAddress iPAddress
        {
            get => this.ipadress;
            set
            {
                this.ipadress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IP"));
            }
        }
    
        public bool IsSelected
        {
            get => this.selected;
            set
            {
                this.selected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IP"));
            }
        }

        public string GetIpString => ipadress.ToString();
    }

}
