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
                    IpSelect ipSelect = new IpSelect() { BroadcastReply = broadcastReply, IsSelected = false };
                    //if not
                    if (MonchaHub.CheckDeviceIP(ipSelect.GetIP) == false)
                    {
                        this.NewDevices.Add(ipSelect);
                    }
                }
                this.OldDevices.Clear();
                foreach (MonchaDevice monchaDevice in MonchaHub.Devices)
                {
                    this.OldDevices.Add(new IpSelect() { BroadcastReply = monchaDevice.BroadcastReply, IsSelected = true });
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
                    MonchaHub.Devices.Add(MonchaHub.ConnectDevice(device.BroadcastReply));
                }
            }

            foreach (IpSelect device in MonchaDeviceList.Items)
            {
                if (device != null && device.IsSelected == false)
                {
                    MonchaHub.RemoveDevice(device.GetIP);
                }
            }

            MonchaHub.RefreshDevice();
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddVirtualBtn_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }
    }

    public class IpSelect : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private BroadcastReply2 broadcastReply;
        private bool selected;
        public BroadcastReply2 BroadcastReply
        {
            get => this.broadcastReply;
            set
            {
                this.broadcastReply = value;
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

        public IPAddress GetIP => new IPAddress(BitConverter.GetBytes(broadcastReply.ipv4));



    }

}
