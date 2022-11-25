using CadProjectorSDK;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Scenes.Commands;
using CadProjectorViewer.EthernetServer.Servers;
using CadProjectorViewer.ToCommands;
using CadProjectorViewer.ToCommands.MainAppCommand;
using CadProjectorViewer.ViewModel;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WatsonTcp;

namespace CadProjectorViewer.EthernetServer
{
    public class ToCutEthernetHub : ObservableCollection<ToCutServerObject>, INotifyPropertyChanged
    {
        public event EventHandler<ReceivedCookies> CommandDummyIncomming;

        public ToCutServerObject SelectServerObject
        {
            get => selectserverobject;
            set
            {
                selectserverobject = value;
                OnPropertyChanged(nameof(SelectServerObject));
            }
        }
        private ToCutServerObject selectserverobject;

        public List<IToCUTServer> ServerType { get; }

        public ObservableCollection<UnicastIPAddressInformation> NetworkInterfaces { get; } = new(TCPTools.GetInterfaces());

        public UnicastIPAddressInformation SelectAddress
        {
            get => _selectaddress;
            set
            {
                _selectaddress = value;
                this.SelectAddressString = _selectaddress.Address.ToString();
                OnPropertyChanged(nameof(SelectAddress));
                OnPropertyChanged(nameof(SelectAddressString));
            }
        }
        private UnicastIPAddressInformation _selectaddress;

        public string SelectAddressString { get; set; }

        public ProjectionScene SelectedScene { get; set; }

        public IToCUTServer SelectType { get; set; }

        public int SelectPort { get; set; }

        public ToCutEthernetHub()
        {
            this.ServerType = new List<IToCUTServer>()
            {
                new UdpCutServer("127.0.0.1", 0),
                new TcpCutServer("127.0.0.1", 0, false)
            };
        }

        public ICommand AddServerCommand =>
            new ActionCommand(() => {
                if (this.SelectedScene != null &&
                this.SelectAddress != null)
                {
                    AddServer(this.SelectedScene, this.SelectPort, this.SelectAddressString);
                }

            });

        public void AddServer(ProjectionScene scene, int port, string ipadress)
        {
            if (this.FirstOrDefault(e => e.IpAdress == ipadress && e.Port == port) == null)
            {
                if (SelectType is IToCUTServer type)
                {
                    IToCUTServer new_server = type.GetCUTServer(ipadress, port);
                    this.Add(new ToCutServerObject(new_server, new SceneModel(scene)));
                }
            }

            OnPropertyChanged(nameof(Servers));
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }
}
