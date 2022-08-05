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
using System.IO;
using System.Xml.Linq;

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

        public IRenderingDevice RenderDevice => (IRenderingDevice)this.DataContext;

        public CanvasDisplaySetting Setting = new CanvasDisplaySetting();

        public override void EndInit()
        {
            base.EndInit();
            if (CanvasDisplaySetting.Load(RenderDevice) is CanvasDisplaySetting displaySetting)
                this.Setting = displaySetting;
        }
    }

    public class CanvasDisplaySetting : INotifyPropertyChanged
    {
        public bool ShowMass { get; set; } = false;
        public bool ShowBlank { get; set; } = false;
        public double Thinkess { get; set; } = AppSt.Default.default_thinkess_percent;
        public Brush Background { get; set; } = Brushes.WhiteSmoke;

        public static void Save()
        {

        }

        public static CanvasDisplaySetting Load(IRenderingDevice renderingDevice)
        {
            CanvasDisplaySetting out_setting = null;

            if (renderingDevice != null)
            {
                string Configpath = $"Config\\{renderingDevice.Uid}.2dsp";

                if (File.Exists(Configpath))
                {
                    XDocument config = XDocument.Load(Configpath);
                }
            }

            return out_setting;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }

}
