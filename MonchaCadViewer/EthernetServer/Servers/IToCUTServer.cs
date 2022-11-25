using CadProjectorViewer.ToCommands;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.EthernetServer.Servers
{
    public interface IToCUTServer
    {
        public event EventHandler<ReceivedCookies> CommandDummyIncomming;

        public string TypeName { get; }
        public bool AutoStart { get; set; }
        public bool IsListening { get; }
        public string IpAddress { get; }
        public int Port { get; set; }

        public void Start();
        public void Stop();

        public void SendMessage(string message, string ip, int port);

        public IToCUTServer GetCUTServer(string ip, int port);
    }

    public struct ReceivedCookies
    {
        public IEnumerable<CommandDummy> Dummies { get; }
        public string ClientIp { get; }
        public int ClientPort { get; }

        public ReceivedCookies(int port, string ip, IEnumerable<CommandDummy> dummies)
        {
            ClientPort = port;
            ClientIp = ip;
            Dummies = dummies;
        }
    }
}
