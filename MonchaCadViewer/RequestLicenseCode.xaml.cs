using CadProjectorSDK.Tools;
using CadProjectorViewer.EthernetServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для RequestLicenseCode.xaml
    /// </summary>
    public partial class RequestLicenseCode : Window
    {
        public TcpListener tcpListener { get; set; }

        public UnicastIPAddressInformation selectUnicast
        {
            get => unicastIP;
            set
            {
                unicastIP = value;
                if (unicastIP != null)
                {
                    StartTCP(unicastIP.Address);
                }
            }
        }
        private UnicastIPAddressInformation unicastIP;

        private List<UnicastIPAddressInformation> IPAddressInformation { get; } = new ();

        private LockKeys LKey => (LockKeys)this.DataContext;

        public RequestLicenseCode()
        {
            InitializeComponent();

            IPAddressInformation = TCPTools.GetInterfaces();
            IPCombo.ItemsSource = IPAddressInformation;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.F10 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                LKey.SetKey();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(LKey.RequestKey);
        }

        private void Pate_Click(object sender, RoutedEventArgs e)
        {
            LKey.LicenseKey = Clipboard.GetText();
        }

        private async void StartTCP(IPAddress iPAddress)
        {
            try
            {
                if (tcpListener != null) tcpListener.Stop();
                int port = TCPTools.FreeTcpPort(iPAddress);
                PortLabel.Content = port.ToString();

                tcpListener = new TcpListener(port);
                tcpListener.Start();

                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                NetworkStream networkStream = client.GetStream();

                byte[] request = System.Text.Encoding.UTF8.GetBytes(LKey.RequestKey);
                await networkStream.WriteAsync(request, 0, request.Length);

                byte[] inputbyte = new byte[100];
                await networkStream.ReadAsync(inputbyte, 0, 100);

                string key = Encoding.UTF8.GetString(inputbyte).Trim('\0');
                this.LKey.Add(key);
                Console.WriteLine(key);

                networkStream.Close();
                client.Close();
            }
            catch
            {
                PortLabel.Content = "TCP ERROR";
            }
        }

        private void LicenseWindow_Closing(object sender, CancelEventArgs e)
        {
            if (tcpListener != null) tcpListener.Stop();
        }
    }

    public class IpImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UnicastIPAddressInformation addressInformation)
            {
                return TCPTools.GetQR(addressInformation.Address.ToString());
            }
            return new BitmapImage();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
