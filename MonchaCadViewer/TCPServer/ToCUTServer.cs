﻿using CadProjectorSDK;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Scenes.Commands;
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
        public event EventHandler<ISceneCommand> SendedCommand;

        public SceneModel MScene { get; }

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
            string[] split = message.Split(new[] { ':' }, 2);
            if (split.Length > 0)
            {
                ISceneCommand sceneCommand = null;
                switch (split[0])
                {
                    case "NEXT":
                        sceneCommand = new SelectNextCommand(true, MScene.Scene);
                        break;
                    case "PREV":
                        sceneCommand = new SelectNextCommand(false, MScene.Scene);
                        break;
                    case "MOVE":
                        Point point = ParseMovePoint(split[1]);
                        sceneCommand = new MovingCommand(MScene.Scene, point.X, point.Y);
                        break;
                }
                SendedCommand?.Invoke(this, sceneCommand);
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



        public ToCUTServer(SceneModel scene)
        {
            this.MScene = scene;
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
