using CadProjectorSDK;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Scenes.Commands;
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
        public ProjectionScene Scene { get; }

        public bool IsConnected => server.Connections > 0;

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
            string[] split = message.Split(new[] { ':' }, 2);
            if (split.Length > 0)
            {
                switch (split[0])
                {
                    case "NEXT":
                        Scene.HistoryCommands.Add(new SelectNextCommand(true, Scene));
                        break;
                    case "PREV":
                        Scene.HistoryCommands.Add(new SelectNextCommand(false, Scene));
                        break;
                    case "MOVE":
                        Point point = ParseMovePoint(split[1]);
                        Scene.HistoryCommands.Add(new MovingCommand(Scene, point.X, point.Y));
                        break;
                }
            }
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



        public ToCUTServer(ProjectionScene scene)
        {
            this.Scene = scene;
            this.server = new SimpleTcpServer("192.168.33.10:9000");
        }

        public void Stop() => server.Stop();

        public void Start() => server.Start();
    }
}
