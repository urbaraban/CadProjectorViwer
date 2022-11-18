﻿using MonchaNETDll.MonchaBroadcast;
using CadProjectorSDK;
using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Net.NetworkInformation;
using CadProjectorSDK.Tools;
using CadProjectorSDK.Interfaces;

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для DeviceManaged.xaml
    /// </summary>
    public partial class LaserSearcher : Window
    {
        private ProjectorHub ProjectorHub { get; set; }

        private List<BroadcastReply2> iPs = new List<BroadcastReply2>();
        public List<IpSelect> OldDevices = new List<IpSelect>();
        public List<IpSelect> NewDevices = new List<IpSelect>();

        public LaserSearcher(ProjectorHub ProjectorHub)
        {
            InitializeComponent();
            this.ProjectorHub = ProjectorHub;
            this.DataContext = ProjectorHub;
            RefreshList();
        }

        private void RefreshList()
        {
            this.iPs = ProjectorSearch.GetMonchaBroadcastReplies();

            if (this.iPs.Count > 0)
            {
                this.NewDevices.Clear();
                foreach (BroadcastReply2 broadcastReply in iPs)
                {
                    IpSelect ipSelect = new IpSelect() {
                        iPAddress = new IPAddress(BitConverter.GetBytes(broadcastReply.ipv4)),
                        DvcType = DeviceType.MonchaNET,
                        IsSelected = false
                    };
                    if (ProjectorHub.CheckDeviceInHub(ipSelect.iPAddress) == false)
                    {
                        this.NewDevices.Add(ipSelect);
                    }
                }
                if (this.NewDevices.Count > 0)
                {
                    FoundDeviceList.ItemsSource = this.NewDevices;
                }
            }

            this.OldDevices.Clear();
            foreach (LProjector monchaDevice in ProjectorHub.Projectors)
            {
                if (monchaDevice is IConnected connected)
                {
                    this.OldDevices.Add(new IpSelect() { iPAddress = connected.IPAddress, IsSelected = true });
                }
            }
            if (this.OldDevices.Count > 0)
            {
                MonchaDeviceList.ItemsSource = this.OldDevices;
            }
        }

        private async void DeviceManagedForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (IpSelect device in FoundDeviceList.Items)
            {
                if (device != null && device.IsSelected == true)
                {
                    ProjectorHub.Projectors.Add(await DevicesMg.GetDeviceAsync(device.iPAddress, device.DvcType, ProjectorHub.Projectors.Count));
                }
            }

            foreach (IpSelect device in MonchaDeviceList.Items)
            {
                if (device != null && device.IsSelected == false)
                {
                    ProjectorHub.Projectors.RemoveDevice(device.iPAddress);
                }
            }
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddVirtualBtn_Click(object sender, RoutedEventArgs e)
        {
            ProjectorHub.Projectors.Add(new VirtualProjector());
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

        public DeviceType DvcType { get; set; }

        public string GetIpString => ipadress.ToString();
    }

}
