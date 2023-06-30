using CadProjectorViewer.Panels.RightPanel.Logs;
using CadProjectorViewer.Services;
using System.Windows.Controls;
using System.Windows.Input;

namespace CadProjectorViewer.Panels.RightPanel
{
    /// <summary>
    /// Логика взаимодействия для LogPanel.xaml
    /// </summary>
    public partial class LogPanel : UserControl
    {
        public LogList LogList => LogList.Instance;

        public LogPanel()
        {
            InitializeComponent();
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
}
