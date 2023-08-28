using CadProjectorViewer.Interfaces;
using CadProjectorViewer.ToCommands;
using CadProjectorViewer.ViewModel;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Xml.Serialization;
using static CadProjectorViewer.Interfaces.IToRemoveObject;

namespace CadProjectorViewer.EthernetServer.Servers
{
    public class ToCutServerObject : NotifyModel, IToRemoveObject
    {
        public event EventHandler<ReceivedCookies> CommandDummyIncomming;

        [XmlIgnore]
        public RemoveDelegate Remove { get; set; }
        public Guid Guid { get; } = Guid.NewGuid();

        public string ObjectName => commandObject.Name;
        public int Port => server.Port;
        public string IpAdress => server.IpAddress;
        public string ServerType => server.TypeName;

        public bool IsListening => server.IsListening;

        private IToCUTServer server { get; }
        private IToCutCommandObject commandObject { get; }

        public ToCutServerObject() { }
        public ToCutServerObject(IToCUTServer server, IToCutCommandObject commandObject)
        {
            this.server = server;
            this.commandObject = commandObject;
        }

        private void Server_CommandDummyIncomming(object sender, ReceivedCookies e)
        {
            foreach (var dummy in e.Dummies)
            {
                CheckDummmy(dummy, e);
            }
        }

        private void CheckDummmy(CommandDummy dummy, ReceivedCookies cookies)
        {
            if (commandObject.GetCommand(dummy) is IToCommand toCommand)
            {
                IToCommand command = toCommand.MakeThisCommand(this.commandObject, dummy.Message);
                ExecutCommand(command, cookies);
            }
            else
            {
                ReceivedCookies receivedCookies = new ReceivedCookies(cookies.ClientIp, cookies.ClientPort, new List<CommandDummy>() { dummy });
                CommandDummyIncomming?.Invoke(this, receivedCookies);
            }
        }

        private void ExecutCommand(IToCommand toCommand, ReceivedCookies cookies)
        {
            object result = toCommand.Run();

            if (toCommand.ReturnRequest == true && result is string message)
            {
                this.SendMessage(message, cookies);
            }
            else if (result is CommandDummy dummy)
            {
                CheckDummmy(dummy, cookies);
            }
        }

        public ICommand StartCommand => new ActionCommand(() => {
            this.server.CommandDummyIncomming += Server_CommandDummyIncomming;
            server.Start();
            OnPropertyChanged(nameof(IsListening));
        });
        public ICommand StopCommand => new ActionCommand(() => {
            this.server.CommandDummyIncomming -= Server_CommandDummyIncomming;
            this.server.Stop();
            OnPropertyChanged(nameof(IsListening));
        });
        public ICommand RemoveCommand => new ActionCommand(() => {
            StopCommand.Execute(this);
            Remove?.Invoke(this);
        });

        public void SendMessage(string message, ReceivedCookies receivedCookies)
        {
            this.server.SendMessage(message, receivedCookies.ClientIp, receivedCookies.ClientPort);
        }
    }
}
