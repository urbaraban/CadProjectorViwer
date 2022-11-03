using CadProjectorViewer.TCPServer;
using Kompas6API7;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CadProjectorViewer.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для ManipulatorTCPDialog.xaml
    /// </summary>
    public partial class ManipulatorTCPDialog : Window, INotifyPropertyChanged
    {
        private ToCUTServer cUTServer { get; }

        public ObservableCollection<UnicastIPAddressInformation> NetworkInterfaces { get; } = new(TCPTools.GetInterfaces());
        public UnicastIPAddressInformation SelectAddress
        {
            get => cUTServer.ServerAddress ?? NetworkInterfaces[0];
            set
            {
                cUTServer.ServerAddress = value;
                Port = TCPTools.FreeTcpPort(cUTServer.ServerAddress.Address);
                OnPropertyChanged("SelectAddress");
                OnPropertyChanged("Port");
            }
        }
        private UnicastIPAddressInformation _selectaddress;

        public int Port
        {
            get => cUTServer.Port;
            set
            {
                cUTServer.Port = value;
                OnPropertyChanged("Port");
            }
        }

        public bool IsListening => cUTServer.IsListening;

        public BitmapSource IPQP => TCPTools.GetQR(this.SelectAddress.Address, this.Port);

        public ManipulatorTCPDialog(ToCUTServer toCUTServer)
        {
            cUTServer = toCUTServer;
            InitializeComponent();
        }

        public ICommand StartManipulatorServer => new ActionCommand(() => {
            cUTServer.ServerAddress = this.SelectAddress;
            if (this.Port < 8000) this.Port = TCPTools.FreeTcpPort(this.SelectAddress.Address);
            cUTServer.Port = this.Port;
            cUTServer.Start();
            OnPropertyChanged(nameof(IsListening));
            if (cUTServer.IsListening == true)
            {
                OnPropertyChanged(nameof(IPQP));
            }
        });

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }
}
