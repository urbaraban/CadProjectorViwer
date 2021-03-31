using System;
using System.Collections.Generic;
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

namespace MonchaCadViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для ProgressPanel.xaml
    /// </summary>
    public partial class ProgressPanel : UserControl
    {
        public static event EventHandler<string> ChangeLabel;
        public static event EventHandler<int> ChangeMaximum;
        public static event EventHandler<int> ChangeStatus;

        public static string Label
        {
            set
            {
                ChangeLabel?.Invoke(null, value);
            }
        }

        public static int MaxValue
        {
            set
            {
                ChangeMaximum?.Invoke(null, value);
            }
        }

        public static int StatValue
        {
            set
            {
                ChangeStatus?.Invoke(null, value);
            }
        }


        public ProgressPanel()
        {
            InitializeComponent();

            ProgressPanel.ChangeLabel += ProgressPanel_ChangeLabel;
            ProgressPanel.ChangeMaximum += ProgressPanel_ChangeMaximum;
            ProgressPanel.ChangeStatus += ProgressPanel_ChangeStatus;
        }

        private void ProgressPanel_ChangeStatus(object sender, int e)
        {
            this.Dispatcher.Invoke(() => this.BarLine.Value = e);
            
        }

        private void ProgressPanel_ChangeMaximum(object sender, int e)
        {
            this.Dispatcher.Invoke(() => this.BarLine.Maximum = e);
        }

        private void ProgressPanel_ChangeLabel(object sender, string e)
        {
            this.Dispatcher.Invoke(() => this.BarLabel.Content = e);
        }

        public static void SetProgressBar(int Value, int MaxValue, string Text)
        {
            ProgressPanel.Label = Text;
            ProgressPanel.MaxValue = MaxValue;
            ProgressPanel.StatValue = Value;
        }

        public static void End()
        {
            //ProgressPanel.Label = string.Empty;
            ProgressPanel.MaxValue = 0;
            ProgressPanel.StatValue = 0;
        }
    }
}
