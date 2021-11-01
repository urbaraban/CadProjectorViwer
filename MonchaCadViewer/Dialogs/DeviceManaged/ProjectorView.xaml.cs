using CadProjectorViewer.CanvasObj;
using CadProjectorSDK.Device;
using CadProjectorSDK.CadObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CadProjectorSDK.CadObjects.LObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;

namespace CadProjectorViewer.DeviceManaged
{
    /// <summary>
    /// Логика взаимодействия для ProjectorView.xaml
    /// </summary>
    
    public partial class ProjectorView : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public ProjectorView() => InitializeComponent();

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.DragMove();
            this.WindowState = WindowState.Maximized;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class LObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UidObject uidObject)
            {
                if (uidObject is CadLine cadLine) return new CanvasLine(cadLine);
                else if (uidObject is CadRect3D cadRectangle) return new CanvasRectangle(cadRectangle, cadRectangle.NameID);
                else if (uidObject is IGeometryObject geometry) return new GeometryPreview(uidObject);
                else if (uidObject is IPixelObject pixelObject) return new ImagePreview(uidObject);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

}
