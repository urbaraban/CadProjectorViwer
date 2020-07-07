using MahApps.Metro.Controls;
using MonchaSDK.Object;
using System.Windows;

namespace MonchaCadViewer.Calibration
{
    /// <summary>
    /// Логика взаимодействия для DotEdit.xaml
    /// </summary>
    public partial class DotEdit : Window
    {
        private LPoint3D _point;

        public DotEdit(LPoint3D point)
        {
            _point = point;

            InitializeComponent();
            this.Loaded += DotEdit_Loaded;
        }

        private void DotEdit_Loaded(object sender, RoutedEventArgs e)
        {
            BindPoint();
        }

        private void RadioProp_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
                BindPoint();
        }


        private void BindPoint()
        {
            WidthNum.DataContext =  this._point;
            WidthNum.SetBinding(NumericUpDown.ValueProperty, radioAbs.IsChecked.Value ? "X" : "MX");

            HeightNum.DataContext = this._point;
            HeightNum.SetBinding(NumericUpDown.ValueProperty, radioAbs.IsChecked.Value ? "Y" : "MY");
        }
    }
}
