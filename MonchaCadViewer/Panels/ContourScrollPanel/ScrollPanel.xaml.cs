using CadProjectorViewer.Modeles;
using System.Windows;
using System.Windows.Controls;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для ScrollPanel.xaml
    /// </summary>
    public partial class ScrollPanel : UserControl
    {
        public ScrollPanel()
        {
            InitializeComponent();
        }


        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AppMainModel appMainModel)
            {
                appMainModel.Tasks.Clear();
            }

        }
    }
}
