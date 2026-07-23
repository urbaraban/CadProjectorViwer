using CadProjectorSDK.Scenes;
using CadProjectorViewer.Services;
using MahApps.Metro.Controls;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace CadProjectorViewer.Panels.RightPanel.Configuration
{
    /// <summary>
    /// Логика взаимодействия для SceneSetting.xaml
    /// </summary>
    public partial class SceneSetting : UserControl
    {
        private ProjectionScene Scene => (ProjectionScene)this.DataContext;

        public SceneSetting()
        {
            InitializeComponent();
        }

        private void MashMultiplierUpDn_ValueIncremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            args.Interval = 0;
            if (MashMultiplierUpDn.Value != null)
            {
                MashMultiplierUpDn.Value = MashMultiplierUpDn.Value.Value * 10;
                Scene.Size.M.Set(MashMultiplierUpDn.Value.Value);
            }
        }

        private void MashMultiplierUpDn_ValueDecremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            args.Interval = 0;
            if (MashMultiplierUpDn.Value != null)
            {
                MashMultiplierUpDn.Value = MashMultiplierUpDn.Value.Value / 10;
                Scene.Size.M.Set(MashMultiplierUpDn.Value.Value);
            }
        }

        private void NumericUpDown_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            InputValidation.NumberPerDotValidationTextBox(sender, e);
            if (sender is NumericUpDown && e.Handled == true)
            {
                Keyboard.ClearFocus();
            }
        }
    }

    public class AttachConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value).ToLower() == ((string)parameter).ToLower();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value == true ? parameter : string.Empty;
        }
    }

    public class Splitter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            object[] values = new object[targetTypes.Length];
            for (int i = 0; i < values.Length; i += 1) values[i] = value;
            return values;
        }
    }
}
