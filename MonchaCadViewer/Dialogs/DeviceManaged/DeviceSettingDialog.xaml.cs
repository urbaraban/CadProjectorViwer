using MahApps.Metro.Controls;
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
using CadProjectorSDK.Interfaces;
using System.Globalization;
using System.Net;

namespace CadProjectorViewer.Panels.DevicePanel
{
    /// <summary>
    /// Логика взаимодействия для DeviceSettingDialog.xaml
    /// </summary>
    public partial class DeviceSettingDialog : Window
    {
        private LDevice _device => (LDevice)this.DataContext;


        public DeviceSettingDialog()
        {
            InitializeComponent();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            //ProjectorHub.CanPlay = false;
            if (this._device is IConnected connected)
            {
                connected.IPAddress = new System.Net.IPAddress(new byte[]
                {
                    byte.Parse(IP1.Text),
                    byte.Parse(IP2.Text),
                    byte.Parse(IP3.Text),
                    byte.Parse(IP4.Text)
                });
            }
        }

        private void CheckBtn_Click(object sender, RoutedEventArgs e)
        {
            CheckLabel.IsChecked = ProjectorHub.CheckDeviceIP(new IPAddress(new byte[]
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

    public class IPAdressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IPAddress address)
            {
                int index = int.Parse(parameter.ToString());
                return address.GetAddressBytes()[index].ToString();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;        
        }
    }
}
