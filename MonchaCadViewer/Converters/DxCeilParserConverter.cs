using CadProjectorSDK.Scenes;
using CadProjectorViewer.ViewModel;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CadProjectorViewer.Converters
{
    internal class DxCeilParserConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ScenesCollection sc)
            {
                return new DxCeilViewModel(sc);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
