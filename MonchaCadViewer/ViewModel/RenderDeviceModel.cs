using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.ViewModel
{
    public class RenderDeviceModel
    {
        public IRenderingDevice Rendering { get; }

        public Guid Guid => Rendering.Uid;

        public ObservableCollection<UidObject> uidObjects => Rendering.RenderObjects;

        public ObservableCollection<CadRect3D> masks {
            get
            {
                if (Rendering is ProjectionScene scene)
                {
                    return scene.Masks;
                }
                return null;
            }
        }

        public CadRect3D Size => Rendering.Size;

        public double HeightResolution => Rendering.HeightResolution;
        public double WidthResolutuon => Rendering.WidthResolutuon;

        public ViewDisplaySetting displaySetting { get; }

        public RenderDeviceModel(IRenderingDevice rendering)
        {
            this.Rendering = rendering;
            this.displaySetting = ViewDisplaySetting.Load(this.Rendering);
        }
    }

    public class ViewDisplaySetting : INotifyPropertyChanged
    {
        public bool ShowMass { get; set; } = false;
        public bool ShowBlank { get; set; } = false;
        public double Thinkess { get; set; } = AppSt.Default.default_thinkess_percent;
        public Brush Background { get; set; } = Brushes.WhiteSmoke;

        public static void Save()
        {

        }

        public static ViewDisplaySetting Load(IRenderingDevice renderingDevice)
        {
            ViewDisplaySetting out_setting = null;

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
