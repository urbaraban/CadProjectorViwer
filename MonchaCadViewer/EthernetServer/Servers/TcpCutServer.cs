using CadProjectorViewer.ToCommands;
using CadProjectorViewer.ViewModel.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public void Start()
        {
            server.Events.ClientConnected += Events_ClientConnected;
            server.Events.MessageReceived += Events_MessageReceived;
            server.Start();
            Thread.Sleep(100);
        }

        private void Events_ClientConnected(object sender, ConnectionEventArgs e)
        {
            LogList.Instance.PostLog(e.Client.IpPort, "tcpServer");
        }

        private void Events_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Data);
            IEnumerable<CommandDummy> dummies = ToCommand.ParseDummys(message);
            ReceivedCookies receivedCookies = new ReceivedCookies(e.Client.IpPort, dummies);
            CommandDummyIncomming?.Invoke(this, receivedCookies);
        }

        public void Stop()
        {
            if (server.IsListening == true)
            {
                server.Events.MessageReceived -= Events_MessageReceived;
                server.Stop();
            }
        }

        public IToCUTServer GetCUTServer(string ip, int port)
        {
            return new TcpCutServer(ip, port, false);
        }
    }
}
