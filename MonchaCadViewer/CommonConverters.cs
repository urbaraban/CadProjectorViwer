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
using CadProjectorViewer.ViewModel;
using System.Windows.Media;

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

    public class CadObjectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is UidObject uidObject
                && values[1] is RenderDeviceModel renderingDevice
                && values[2] is ScaleTransform transform)
            {
                if (CanvasObjectSwitch(uidObject) is CanvasObject canvasObject)
                {
                    canvasObject.GetFrameTransform = () => transform;
                    canvasObject.GetViewModel = () => renderingDevice;
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
