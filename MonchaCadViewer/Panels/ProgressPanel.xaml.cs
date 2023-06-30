using CadProjectorViewer.Services;
using System;
using System.Windows.Controls;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для ProgressPanel.xaml
    /// </summary>
    public partial class ProgressPanel : UserControl
    {

        public ProgressPanel()
        {
            InitializeComponent();
            this.DataContext = Progress.Instance;
        }
    }
}
