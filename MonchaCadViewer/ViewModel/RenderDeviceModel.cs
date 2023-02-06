using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.ViewModel
{
    internal class RenderDeviceModel : NotifyModel
    {
        public IRenderingDisplay RenderingDisplay { get; }

        public Guid Guid => RenderingDisplay.Uid;

        public ObservableCollection<UidObject> uidObjects => RenderingDisplay.RenderObjects;

        public ObservableCollection<CadRect3D> masks {
            get
            {
                if (RenderingDisplay is ProjectionScene scene)
                {
                    return scene.Masks;
                }
                return null;
            }
        }

        public CadRect3D Size => RenderingDisplay.Size;

        public virtual bool ShowHide
        {
            get => showhide;
            set
            {
                showhide = value;
                OnPropertyChanged("ShowHide");
            }
        }
        private bool showhide;

        public virtual double Width
        {
            get => widthresolution;
            set
            {
                widthresolution = value;
                OnPropertyChanged("Width");
            }
        }
        private double widthresolution = 1000;

        public virtual double Height
        {
            get => heighthresolution;
            set
            {
                heighthresolution = value;
                OnPropertyChanged("Height");
            }
        }
        private double heighthresolution = 1000;

        public virtual double ThinkessPersent { get; } = 0.001;

        public virtual double Thinkess => Math.Max(Width, Height) * ThinkessPersent;

        public ViewDisplaySetting displaySetting { get; }

        public RenderDeviceModel(IRenderingDisplay renderingDisplay)
        {
            this.RenderingDisplay = renderingDisplay;
            this.displaySetting = ViewDisplaySetting.Load(this.RenderingDisplay);
        }

        public System.Windows.Point GetPoint(double X, double Y)
        {
            return new System.Windows.Point(
                X * this.Width,
                Y * this.Height);
        }

        public System.Windows.Point GetProportion(double X, double Y)
        {
            return new System.Windows.Point(
                (X - this.Size.MX) / this.Size.MWidth, 
                (Y - this.Size.MX) / this.Size.MHeight);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
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

        public static ViewDisplaySetting Load(IRenderingDisplay renderingDevice)
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
