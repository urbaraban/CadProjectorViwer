using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonTcp;

namespace CadProjectorViewer.EthernetServer.Servers
{
    internal class TcpCutServer : IToCUTServer
    {
        public event EventHandler<ReceivedCookies> CommandDummyIncomming;

        private WatsonTcpServer server { get; }

        public string TypeName => "TCP";

        public bool AutoStart 
        {
            get => autostart;
            set => autostart = value;
        }
        private bool autostart;

        public bool IsListening => server.IsListening;

        public string IpAddress { get; }

        public int Port { get; set; }

        public TcpCutServer(string ipAddress, int port, bool autostart)
        {
            this.IpAddress = ipAddress;
            this.Port = port;
            if (port != 0)
            {
                this.server = new WatsonTcpServer(ipAddress, port);
                this.AutoStart = autostart;
            }
        }

        public void SendMessage(string message, string ip, int port)
        {
            this.server.Send($"{ip}:{port}", message);
        }

        public void Start() => server.Start();

        public void Stop() => server.Stop();

        public IToCUTServer GetCUTServer(string ip, int port)
        {
            return new TcpCutServer(ip, port, false);
        }
    }
}
