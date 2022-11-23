using CadProjectorSDK;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorViewer.ToCommands;
using CadProjectorViewer.ToCommands.MainAppCommand;
using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WatsonTcp;

namespace CadProjectorViewer.TCPServer
{
    public class ToCUTServer
    {
        internal event EventHandler<IEnumerable<CommandDummy>> CommandDummyIncomming;

        internal AppMainModel MainModel { get; }

        public bool IsListening => server != null && server.IsListening;

        public UnicastIPAddressInformation ServerAddress { get; set; }
        public int Port { get; set; }

        private WatsonTcpServer server
        {
            get => _server;
            set
            {
                if (_server != null)
                {
                    _server.Events.MessageReceived -= Events_MessageReceived;
                    _server.Events.ClientConnected -= Events_ClientConnected;
                }
                _server = value;
                _server.Events.MessageReceived += Events_MessageReceived; ;
                _server.Events.ClientConnected += Events_ClientConnected;
            }
        }

        private void Events_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Data, 0, e.Data.Length);
            if (string.IsNullOrEmpty(message) == false)
            {
                IEnumerable<CommandDummy> commands = ToCommand.ParseDummys(message);
                CommandDummyIncomming?.Invoke(e, commands);
            }
        }

        private WatsonTcpServer _server;

        private void Events_ClientConnected(object sender, ConnectionEventArgs e)
        {
            
        }

        public bool SendRequest(string Message, object requestdata)
        {
            if (requestdata is MessageReceivedEventArgs RecievedData)
            {
                return SendMessage(Message, RecievedData.Client.IpPort);
            }
            return false;
        }

        public bool SendMessage(string Message, string ipport)
        {
            this.server.Send(ipport, Message);

            return true;
        }

        private Point ParseMovePoint(string message)
        {
            Point point = new Point();
            try
            {
                foreach (string str in message.Split(new[] { ';' }, 2))
                {
                    string[] coord = str.Split(new[] { ':' }, 2);
                    switch (coord[0])
                    {
                        case "X":
                            point.X = double.Parse(coord[1]);
                            break;
                        case "Y":
                            point.Y = double.Parse(coord[1]);
                            break;
                    }
                }
            }
            catch { }

            return point;
        }

        public void Stop()
        {
            if (this.server != null && this.server.IsListening == true)
                server.Stop();
        }

        public void Start()
        {
            if (server != null) Stop();
            this.server = new WatsonTcpServer(this.ServerAddress.Address.ToString(), this.Port);
            server.Start();
        }
    }
}
