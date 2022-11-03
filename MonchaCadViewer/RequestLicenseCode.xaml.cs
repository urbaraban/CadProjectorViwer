using CadProjectorSDK;
using CadProjectorSDK.Tools;
using CadProjectorViewer.TCPServer;
using OpenCvSharp.WpfExtensions;
using QRCoder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit.Primitives;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

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

        private LockKey LKey => (LockKey)this.DataContext;

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
                return TCPTools.GetQR(addressInformation.Address);
            }
            return new BitmapImage();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
