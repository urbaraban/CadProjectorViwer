﻿using MahApps.Metro.Controls;
using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.Panels.DevicePanel;
using CadProjectorSDK;
using CadProjectorSDK.Device;
using System;
using System.Collections.Generic;
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

using AppSt = CadProjectorViewer.Properties.Settings;
using CadProjectorSDK.Device.Controllers;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для DeviceTab.xaml
    /// </summary>
    public partial class DeviceTab : UserControl
    {
        private LDevice device => (LDevice)DeviceCombo.SelectedItem;

        public DeviceTab()
        {
            InitializeComponent();
        }

        private void LMeter_ChangeDimention(object sender, double e)
        {
           /* DistanceLabel.Invoke(() => {
                DistanceLabel.Content = e;
            });*/
        }

        private void LaserMeterToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (LaserMetersCombo.SelectedItem is VLTLaserMeters laserMeters)
            {
                laserMeters.Turn(LaserMeterToggle.IsOn);
            }
        }

        private void ScanRateRealSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                Point point = Mouse.GetPosition(slider);
                slider.Value = slider.Minimum + (point.X / slider.ActualWidth) * (slider.Maximum - slider.Minimum);
            }
        }

        private void DeviceSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCombo.SelectedItem != null)
            {
                DeviceSettingDialog deviceSettingDialog = new DeviceSettingDialog((LDevice)DeviceCombo.SelectedItem);
                deviceSettingDialog.Show();
            }
        }


        private void MeshSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            CreateGridWindow createGridWindow = new CreateGridWindow(this.device.SelectMesh);
            createGridWindow.Show();
        }

        private void MeshListBtn_Click(object sender, RoutedEventArgs e)
        {
            MeshesDialog meshesDialog = new MeshesDialog() { DataContext = this.device };
            meshesDialog.Show();
        }

        public void DeviceBright(double TenPercent)
        {
            this.device.Alpha = (byte)(255 * TenPercent / 10);
        }

    }
}
