using CadProjectorSDK;
using CadProjectorViewer.ViewModel;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CadProjectorViewer.Converters
{
    internal class ProjectorHubConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProjectorHub hub)
            {
                return new ScrollPanelViewModel(hub);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
