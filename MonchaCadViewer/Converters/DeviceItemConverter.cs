using CadProjectorViewer.ViewModel.Devices;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CadProjectorViewer.Converters
{
    internal class DeviceItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CadProjectorSDK.Device.LProjector projector)
            {
                return new DeviceItemViewModel(projector);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
