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
using CadProjectorSDK.CadObjects.Interfaces;
using System.Windows.Controls;
using AppSt = CadProjectorViewer.Properties.Settings;
using System.Windows;

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

    public class GetCadCanvas : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CadCanvas cadCanvas = new CadCanvas();
            if (value is IRenderingDevice renderingDevice)
                cadCanvas = new CadCanvas() { DataContext = renderingDevice };
            return cadCanvas;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class CadObjectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is UidObject uidObject
                && values[1] is CadCanvas canvas
                && values[2] is FrameworkElement frameworkElement)
            {
                if (CanvasObjectSwitch(uidObject) is CanvasObject canvasObject)
                {
                    canvasObject.GetResolution = () => new Tuple<double, double>(canvas.Width, canvas.Height);
                    canvasObject.GetContainer = () => frameworkElement;
                    canvasObject.GetCanvas = () => canvas;
                    //canvasPanel.SizeChange += canvasObject.ParentChangeSize;
                    return canvasObject;
                }

            }
            return values;

            CanvasObject CanvasObjectSwitch(UidObject uidObject)
            {
                if (uidObject is ProjectorMesh mesh) return new CanvasMesh(mesh);
                else if (uidObject is CadLine cadLine) return new CanvasLine(cadLine);
                else if (uidObject is CadRect3D cadRectangle) return new CanvasRectangle(cadRectangle, cadRectangle.NameID);
                else return new CanvasObject(uidObject, true);
                return null;
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
