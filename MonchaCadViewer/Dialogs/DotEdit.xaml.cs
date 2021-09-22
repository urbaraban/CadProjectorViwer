using MahApps.Metro.Controls;
using CadProjectorSDK.CadObjects;
using System.Windows;

namespace CadProjectorViewer.Calibration
{
    /// <summary>
    /// Логика взаимодействия для DotEdit.xaml
    /// </summary>
    public partial class DotEdit : Window
    {
        private CadPoint3D _point;

        public DotEdit(CadPoint3D point)
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
