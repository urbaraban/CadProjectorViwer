﻿using MahApps.Metro.Controls;
using CadProjectorViewer.CanvasObj;
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
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels.DevicePanel
{
    /// <summary>
    /// Логика взаимодействия для DeviceSettingDialog.xaml
    /// </summary>
    public partial class DeviceSettingDialog : Window
    {
        private LDevice _device => (LDevice)this.DataContext;

        public event EventHandler<List<FrameworkElement>> DrawObjects;

        public DeviceSettingDialog()
        {
            InitializeComponent();

            if (this._device != null)
            {
                IP1.Text = this._device.iPAddress.GetAddressBytes()[0].ToString();
                IP2.Text = this._device.iPAddress.GetAddressBytes()[1].ToString();
                IP3.Text = this._device.iPAddress.GetAddressBytes()[2].ToString();
                IP4.Text = this._device.iPAddress.GetAddressBytes()[3].ToString();
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            //ProjectorHub.CanPlay = false;

            this._device.iPAddress = new System.Net.IPAddress(new byte[]
            {
                byte.Parse(IP1.Text),
                byte.Parse(IP2.Text),
                byte.Parse(IP3.Text),
                byte.Parse(IP4.Text)
            });
        }

        private void CheckBtn_Click(object sender, RoutedEventArgs e)
        {
            CheckLabel.IsChecked = ProjectorHub.CheckDeviceIP(new System.Net.IPAddress(new byte[]
            {
                byte.Parse(IP1.Text),
                byte.Parse(IP2.Text),
                byte.Parse(IP3.Text),
                byte.Parse(IP4.Text)
            }));
        }

        private void CheckIsNumeric(TextCompositionEventArgs e)
        {
            char c = Convert.ToChar(e.Text);
            if (Char.IsNumber(c))
                e.Handled = false;
            else
                e.Handled = true;

            base.OnPreviewTextInput(e);
        }

        private void IP_TextInput(object sender, TextCompositionEventArgs e) => CheckIsNumeric(e);

        private void IP_LostFocus(object sender, RoutedEventArgs e)
        {
           if (sender is TextBox textBox)
            {
                if (int.Parse(textBox.Text) > 255) textBox.Text = "255";
            }
        }

        private void MinusBaseBtn_Click(object sender, RoutedEventArgs e)
        {
            this._device.BaseMesh = null;
        }

        private void MinusSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            this._device.SelectMesh = null;
        }
    }
}