using OpenCvSharp.WpfExtensions;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CadProjectorViewer.EthernetServer
{
    public static class TCPTools
    {
        public static List<UnicastIPAddressInformation> GetInterfaces()
        {
            List<UnicastIPAddressInformation> IPAddressInformation = new();
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface @interface in interfaces)
            {
                foreach (UnicastIPAddressInformation unicastIPAddress in @interface.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        IPAddressInformation.Add(unicastIPAddress);
                    }
                }
            }
            return IPAddressInformation;
        }

        public static int FreeTcpPort(IPAddress iPAddress)
        {
            TcpListener l = new TcpListener(iPAddress, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public static BitmapSource GetQR(string iPAddress, int port = 0)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            string message = $"{iPAddress}:{port}";
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(message, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            return qrCodeImage.ToBitmapSource();
        }

        public static byte[] GetBytes(string message)
        {
            return Encoding.UTF8.GetBytes(message);
        }
    }
}
