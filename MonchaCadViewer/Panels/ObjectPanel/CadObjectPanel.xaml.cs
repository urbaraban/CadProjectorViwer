using CadProjectorViewer.Services;
using MahApps.Metro.Controls;
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для CadObjectPanel.xaml
    /// </summary>
    public partial class CadObjectPanel : UserControl
    {
        public CadObjectPanel()
        {
            InitializeComponent();
        }

        private void NumericUpDown_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            InputValidation.NumberPerDotValidationTextBox(sender, e);
        }
    }

    public class ScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = AppSt.Default.stg_scale_percent == true ? 100 : 1;
            double invert = AppSt.Default.stg_scale_invert == true ? (AppSt.Default.stg_scale_percent == true ? 100 : 1) : 0;
            return Math.Round(Math.Abs(invert - Math.Abs((double)value) * percent), 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = AppSt.Default.stg_scale_percent == true ? 100 : 1;
            double invert = AppSt.Default.stg_scale_invert == true ? (AppSt.Default.stg_scale_percent == true ? 100 : 1) : 0;
            if (value != null) return Math.Abs((invert - (double)value) / percent);
            else return 0;
        }
    }


}
