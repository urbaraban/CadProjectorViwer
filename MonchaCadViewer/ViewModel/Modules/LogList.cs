using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ViewModel.Modules
{
    public class LogList : ObservableCollection<LogMessage>
    {
        private string _filepath;

        public delegate void PostDelegate(LogMessage logMessage);
        public PostDelegate Post { get; set; }

        public LogList(string filepath)
        {
            _filepath = filepath;
        }

        public void PostLog(string msg, string sender)
        {
            LogMessage logMessage = new LogMessage(msg, sender);
            Post?.Invoke(logMessage);
            base.Add(logMessage);
            // add this post in file
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