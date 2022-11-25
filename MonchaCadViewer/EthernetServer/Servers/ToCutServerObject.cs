using CadProjectorViewer.ToCommands;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CadProjectorViewer.EthernetServer.Servers
{
    public class ToCutServerObject
    {
        public string ObjectName => commandObject.Name;
        public int Port => server.Port;
        public string IpAdress => server.IpAddress;
        public string ServerType => server.TypeName;

        public bool IsListening => server.IsListening;

        private IToCUTServer server { get; }
        private IToCutCommandObject commandObject { get; }

        public ToCutServerObject(IToCUTServer server, IToCutCommandObject commandObject)
        {
            this.server = server;
            this.commandObject = commandObject;
        }

        public ICommand StartCommand => new ActionCommand(() => server.Start());
        public ICommand StopCommand => new ActionCommand(() => server.Stop());
    }
}
