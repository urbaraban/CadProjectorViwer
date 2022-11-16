using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CadProjectorViewer.ViewModel.Modules
{
    public class LogList : ObservableCollection<LogMessage>
    {

        private string _filepath;

        private Dispatcher dispatcher { get; }

        public int MessageMaxCount { get; set; } = 30;

        public delegate void PostDelegate(LogMessage logMessage);

        /// <summary>
        /// Make loging module
        /// </summary>
        /// <param name="filepath">path for upload log in file (not working)</param>
        public LogList(string filepath)
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            _filepath = filepath;
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