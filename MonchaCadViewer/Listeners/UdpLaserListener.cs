using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonchaCadViewer.Listeners
{
    public class UdpLaserListener
    {
        public event EventHandler<byte[]> IncomingData;

        public static bool Status { get; set; } = false;

        private UdpClient udpClient;
        private IPEndPoint groupEP;
        private int port;


        public UdpLaserListener(int port)
        {
            if (this.udpClient == null)
            {
                this.port = port;
            }
        }

        public async Task Run()
        {
            UdpLaserListener.Status = true;


            while (UdpLaserListener.Status)
            {
                this.udpClient = new UdpClient(port);
                this.groupEP = new IPEndPoint(IPAddress.Loopback.Address, port);

                try
                {
                    byte[] bytes = await Task<byte[]>.Factory.StartNew(() =>
                    {
                        Console.WriteLine("Waiting for broadcast");
                        return udpClient.Receive(ref groupEP);
                    });
                    IncomingData?.Invoke(this, bytes);
                }
                catch (SocketException er)
                {
                    Console.WriteLine(er);
                }
                finally
                {
                    udpClient.Close();
                }
            }
            udpClient.Close();
        }
    }
}
