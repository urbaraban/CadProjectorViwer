using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.CanvasObj;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

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

    public class CadObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UidObject uidObject)
            {
                if (uidObject is ProjectorMesh mesh) return new CanvasMesh(mesh);
                else if (uidObject is CadLine cadLine) return new CanvasLine(cadLine);
                else if (uidObject is CadRect3D cadRectangle) return new CanvasRectangle(cadRectangle, cadRectangle.NameID);
                else if (uidObject is IGeometryObject geometry) return new GeometryPreview(uidObject);
                else if (uidObject is IPixelObject pixelObject) return new ImagePreview(uidObject);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
