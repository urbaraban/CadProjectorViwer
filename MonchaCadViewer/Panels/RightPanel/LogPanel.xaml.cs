using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CadProjectorViewer.Panels.RightPanel
{
    /// <summary>
    /// Логика взаимодействия для LogPanel.xaml
    /// </summary>
    public partial class LogPanel : UserControl
    {
        public LogPanel()
        {
            InitializeComponent();
        }


        private async void TextBlock_MouseDownAsync(object sender, MouseButtonEventArgs e)
        {

        }
    }

    public class LogList : ObservableCollection<LogMessage>
    {
        public void PostLog(string msg, string sender)
        {
           base.Add(new LogMessage(msg, sender));
        }
    }


    public struct LogMessage
    {
        private string message;
        private string sender;
        private DateTime time;

        public LogMessage(string Message, string Sender)
        {
            this.message = Message;
            this.sender = Sender;
            this.time = DateTime.UtcNow;
        }

        public string Message => message;
        public string Sender => sender;
        public DateTime Time => time;
    }
}
