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

        private bool Fix
        {
            get => _fix;
            set
            {
                _fix = value;
                OnPropertyChanged("Fix");
            }
        }
        private bool _fix = false;

        public ProjectorView()
        {
            InitializeComponent();
        }


        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FixMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Fix = !this.Fix;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.Fix == true)
            {
                this.WindowState = WindowState.Normal;
                this.DragMove();
                this.WindowState = WindowState.Maximized;
                if (this.DataContext is LProjector lDevice)
                {
                    lDevice.WidthResolutuon = this.Width;
                    lDevice.HeightResolution = this.Height;
                }
            }
        }
    }
}
