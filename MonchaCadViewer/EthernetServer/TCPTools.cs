using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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

            System.Drawing.Bitmap bitmap = qrCode.GetGraphic(20);

            return ConvertBitmap(bitmap);
        }

        public static BitmapSource ConvertBitmap(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }

        public static byte[] GetBytes(string message)
        {
            return Encoding.UTF8.GetBytes(message);
        }
    }
}
