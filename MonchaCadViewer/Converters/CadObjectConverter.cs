using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.ViewModel;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CadProjectorViewer.Converters
{
    public class CadObjectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is UidObject uidObject)
            {
                CanvasObject canvasObject = new CanvasObject(uidObject, true);

                if (values[1] is RenderDeviceModel renderDeviceModel)
                {
                    canvasObject.GetViewModel = () => renderDeviceModel;
                }
                if (values[2] is ScaleTransform scaleTransform)
                {
                    canvasObject.GetFrameTransform = () => scaleTransform;
                }
                return canvasObject;
            }
            return values;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
