using System.Windows;

namespace CadProjectorViewer.Dialogs.DeviceManaged
{
    /// <summary>
    /// Логика взаимодействия для AddDeviceByIpDialog.xaml
    /// </summary>
    public partial class AddDeviceByIpDialog : Window
    {
        public AddDeviceByIpDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
