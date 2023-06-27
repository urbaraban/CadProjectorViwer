using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace CadProjectorViewer.ViewModel.Modules
{
    public class LogList : ObservableCollection<LogMessage>
    {
        public static LogList Instance { get; } = new LogList();
        public int MessageMaxCount { get; set; } = 30;

        public delegate void PostDelegate(LogMessage logMessage);

        private Dispatcher dispatcher { get; }

        private LogList()
        {
            dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void PostLog(string msg, string sender)
        {
            LogMessage logMessage = new LogMessage(msg, sender);
            dispatcher.Invoke(() =>
            {
                base.Add(logMessage);
            });
        }

        protected override void InsertItem(int index, LogMessage item)
        {
            base.InsertItem(index, item);
            while (this.Count > MessageMaxCount)
            {
                this.RemoveAt(0);
            }
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