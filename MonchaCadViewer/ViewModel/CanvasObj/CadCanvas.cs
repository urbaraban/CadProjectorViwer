using CadProjectorSDK;
using CadProjectorSDK.Device;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CadProjectorSDK.CadObjects;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using CadProjectorViewer.Panels;
using System.Windows.Data;
using System.Collections.ObjectModel;
using CadProjectorSDK.Device.Mesh;
using System.Globalization;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.CanvasObj
{
    public class CadCanvas : Canvas, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public IRenderingDevice GetActualRenderDevice()
        {
            if (this.DataContext is IRenderingDevice renderDevice)
            {
                return renderDevice;
            }
            return null;
        }

        public double GetThinkess()
        {
            double percent = Math.Max(this.ActualWidth, this.ActualHeight) * AppSt.Default.default_thinkess_percent;
            return Math.Max(1, percent);
        }

        private void LoadSetting()
        {
            this.Background = Brushes.Transparent; //backBrush;
        }
    }

}
