using CadProjectorViewer.Panels.RightPanel.Logs;
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
        public LogList Logs { get; } = new LogList();

        public LogPanel()
        {
            InitializeComponent();
            Logs.Post = PostMessage;
        }


        private void PostMessage(LogMessage logMessage)
        {
            LogListBox.Dispatcher.Invoke(() => { LogListBox.Items.Add(logMessage); });
        }


        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is LogMessage logMessage)
            {
                LogWindow logWindow = new LogWindow() { DataContext = logMessage };
                logWindow.Show();
            }
        }
    }

    public class LogList : ObservableCollection<LogMessage>
    {
        public delegate void PostDelegate(LogMessage logMessage);
        public PostDelegate Post { get; set; }

        public void PostLog(string msg, string sender)
        {
            LogMessage logMessage = new LogMessage(msg, sender);
            Post?.Invoke(logMessage);
            base.Add(logMessage);
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
