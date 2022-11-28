using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using CadProjectorViewer.ToCommands;
using SimpleUdp;

namespace CadProjectorViewer.EthernetServer.Servers
{
    internal class UdpCutServer : IToCUTServer
    { 
        public event EventHandler<ReceivedCookies> CommandDummyIncomming;

        private UdpEndpoint udpEndpoint { get; }

        public string TypeName => "UDP";

        public bool AutoStart 
        {
            get => autostart;
            set
            {
                autostart = value;
                if (value == true && this.IsListening == false)
                {
                    this.Start();
                }
            }
        }
        private bool autostart;

        public bool IsListening => true;

        public string IpAddress { get; }

        public int Port { get; set; } = 11000;

        public UdpCutServer(string address, int port = 11000)
        {
            this.IpAddress = address;
            this.Port = port;
            if (port != 0)
            {
                this.udpEndpoint = new UdpEndpoint(address, port);
                this.udpEndpoint.DatagramReceived += UdpEndpoint_DatagramReceived;
            }
        }

        public UdpCutServer(string address, int port, bool autostart) : this(address, port)
        {
            this.autostart = autostart;
        }

        public void Stop() => udpEndpoint.Stop();

        public void Start() => udpEndpoint.Start();

        private void UdpEndpoint_DatagramReceived(object sender, Datagram e)
        {
            string message = Encoding.UTF8.GetString(e.Data, 0, e.Data.Length);
            if (string.IsNullOrEmpty(message) == false)
            {
                ReceivedCookies cookies = new ReceivedCookies (e.Ip, e.Port, ToCommand.ParseDummys(message));
                CommandDummyIncomming?.Invoke(this, cookies);
            }
        }

        public void SendMessage(string message, string ip, int port)
        {
            this.udpEndpoint.Send(ip, port, message);
        }

        public IToCUTServer GetCUTServer(string ip, int port)
        {
            return new UdpCutServer(ip, port);
        }
    }
}
