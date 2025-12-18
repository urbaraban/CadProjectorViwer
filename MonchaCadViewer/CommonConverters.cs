using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.CanvasObj;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using CadProjectorViewer.ViewModel;
using System.Windows.Media;
using CadProjectorViewer.ViewModel.Scene;

namespace CadProjectorViewer.Converters
{
    public class NotNullBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BoolVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boo)
            {
                return boo ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Visibility.Collapsed;
        }
    }

    public class RoundDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Round((double)value, 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class GetViewModel : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RenderDeviceModel cadCanvas = null;
            if (value is ProjectionScene scene)
            {
                cadCanvas = new SceneModel(scene);
            }
            else if (value is IRenderingDisplay renderingDevice)
            {
                cadCanvas = new RenderDeviceModel(renderingDevice);
            }

            return cadCanvas;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class BoolRegGreenColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b == true) return Brushes.Green;
            else return Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    
}
