using CadProjectorSDK;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorViewer.ToCommands;
using CadProjectorViewer.ToCommands.MainAppCommand;
using CadProjectorViewer.ViewModel;
using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CadProjectorViewer.TCPServer
{
    public class ToCUTServer
    {
        internal event EventHandler<IEnumerable<CommandDummy>> CommandDummyIncomming;

        internal AppMainModel MainModel { get; }

        public bool IsListening => server != null && server.IsListening;

        public UnicastIPAddressInformation ServerAddress { get; set; }
        public int Port { get; set; }

        private SimpleTcpServer server
        {
            get => _server;
            set
            {
                if (_server != null)
                {
                    _server.Events.DataReceived -= Events_DataReceived;
                    _server.Events.ClientConnected -= Events_ClientConnected;
                }
                _server = value;
                _server.Events.DataReceived += Events_DataReceived;
                _server.Events.ClientConnected += Events_ClientConnected;
            }
        }
        private SimpleTcpServer _server;

        private void Events_ClientConnected(object sender, ConnectionEventArgs e)
        {
            
        }

        private void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Data.Array, 0, e.Data.Count);
            if (string.IsNullOrEmpty(message) == false)
            {
                IEnumerable<CommandDummy> commands = ToCommand.ParseDummys(message);
                CommandDummyIncomming?.Invoke(e, commands);
            }
        }

        public bool SendRequest(string Message, object requestdata)
        {
            if (requestdata is DataReceivedEventArgs RecievedData)
            {
                return SendMessage(Message, RecievedData.IpPort);
            }
            return false;
        }

        public bool SendMessage(string Message, string ipport)
        {
            bool connected = this.server.IsConnected(ipport);
            if (connected == true)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(Message);
                this.server.Send(ipport, bytes);
            }
            return connected;
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
            if (this.server != null && this.server.IsConnected(this.ServerAddress.ToString()) == true)
                server.Stop();
        }

        public void Start()
        {
            if (server != null) Stop();
            this.server = new SimpleTcpServer(this.ServerAddress.Address.ToString(), this.Port);
            server.Start();
        }
    }
}
