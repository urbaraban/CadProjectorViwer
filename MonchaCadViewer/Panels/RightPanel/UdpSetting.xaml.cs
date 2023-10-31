using CadProjectorSDK.UDP;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels.RightPanel
{
    /// <summary>
    /// Логика взаимодействия для UdpSetting.xaml
    /// </summary>
    public partial class UdpSetting : UserControl
    {
        private UdpLaserListener udpLaser => (UdpLaserListener)this.DataContext;

        public UdpSetting()
        {
            InitializeComponent();
        }

        private void PortUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            AppSt.Default.ether_udp_port = (int)PortUpDn.Value.Value;
            AppSt.Default.Save();
        }

        public ICommand UdpListenCommand => new ActionCommand(() => {
            udpLaser.Run(AppSt.Default.ether_udp_port);
        });

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ToStringBytes = ByteBox.Text.Replace(" ", "");

                if (ToStringBytes.Length > 0)
                {
                    byte[] b = new byte[ToStringBytes.Length / 2];
                    for (int i = 0; i < b.Length; i += 1)
                    {
                        b[i] = byte.Parse($"{ToStringBytes[i * 2]}{ToStringBytes[i * 2 + 1]}", NumberStyles.HexNumber);
                    }

                    await udpLaser.Read(b);
                }
            }
            catch
            {

            }
        }
    }
}
