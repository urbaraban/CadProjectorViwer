﻿using MahApps.Metro.Controls;
using CadProjectorViewer.CanvasObj;
using CadProjectorSDK;
using CadProjectorSDK.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AppSt = CadProjectorViewer.Properties.Settings;
using CadProjectorSDK.Interfaces;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using CadProjectorSDK.Render;
using CadProjectorSDK.Render.Graph;

namespace CadProjectorViewer.Panels.DevicePanel
{
    /// <summary>
    /// Логика взаимодействия для DeviceSettingDialog.xaml
    /// </summary>
    public partial class DeviceSettingDialog : Window
    {
        private LProjector _device => (LProjector)this.DataContext;

        public int GradientSteps
        {
            get => gradientsteps;
            set
            {
                gradientsteps = value;
                OnPropertyChanged("GradientSteps");
            }
        }
        private int gradientsteps = 10;

        public int GradientStep
        {
            get => gradientstep;
            set
            {
                gradientstep = value;
                OnPropertyChanged("GradientStep");
            }
        }
        private int gradientstep = 1;

        public int GradientCount
        {
            get => gradientcount;
            set
            {
                gradientcount = value;
                OnPropertyChanged("GradientCount");
            }
        }
        private int gradientcount = 1;

        public DeviceSettingDialog()
        {
            InitializeComponent();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            //ProjectorHub.CanPlay = false;
            if (this._device is IConnected connected)
            {
                connected.IPAddress = new System.Net.IPAddress(new byte[]
                {
                    byte.Parse(IP1.Text),
                    byte.Parse(IP2.Text),
                    byte.Parse(IP3.Text),
                    byte.Parse(IP4.Text)
                });
            }
        }

        private void CheckBtn_Click(object sender, RoutedEventArgs e)
        {
            CheckLabel.IsChecked = ProjectorHub.CheckDeviceIP(new IPAddress(new byte[]
            {
                byte.Parse(IP1.Text),
                byte.Parse(IP2.Text),
                byte.Parse(IP3.Text),
                byte.Parse(IP4.Text)
            }));
        }

        private void CheckIsNumeric(TextCompositionEventArgs e)
        {
            char c = Convert.ToChar(e.Text);
            if (Char.IsNumber(c))
                e.Handled = false;
            else
                e.Handled = true;

            base.OnPreviewTextInput(e);
        }

        private void IP_TextInput(object sender, TextCompositionEventArgs e) => CheckIsNumeric(e);

        private void IP_LostFocus(object sender, RoutedEventArgs e)
        {
           if (sender is TextBox textBox)
            {
                if (int.Parse(textBox.Text) > 255) textBox.Text = "255";
            }
        }

        private void MinusSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            this._device.SelectMesh = null;
        }


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this._device != null)
            {
                IList<IRenderedObject> elements = new List<IRenderedObject>();

                double widthstep = 1d / (GradientSteps * 2);
                double heightstep = 1d / (GradientSteps * 2);

                for (int i = 0; i < GradientCount && this.GradientStep + i <= GradientSteps; i += 1)
                {
                    double alreadywstep = Math.Abs(widthstep * (this.GradientStep + i));
                    double alreadyhstep = Math.Abs(heightstep * (this.GradientStep + i));
                    VectorLinesCollection rect = new VectorLinesCollection(CadProjectorSDK.Device.Mesh.MeshType.NONE);

                    /*

                    VectorLine Line2 = new VectorLine(
                            new RenderPoint(0.5 + alreadywstep, 0.5 - alreadyhstep),
                            new RenderPoint(0.5 + alreadywstep, 0.5 + alreadyhstep));
                    elements.Add(Line2.Split(5));

                    VectorLine Line3 = new VectorLine(
                            new RenderPoint(0.5 + alreadywstep, 0.5 + alreadyhstep),
                            new RenderPoint(0.5 - alreadywstep, 0.5 + alreadyhstep));
                    elements.Add(Line3.Split(5));
                      */

                    VectorLine Line4 = new VectorLine(
                            new RenderPoint(0.5 - alreadywstep, 0.5 + alreadyhstep),
                            new RenderPoint(0.5 - alreadywstep, 0.5 - alreadyhstep));
                    elements.Add(Line4.Split(5));

                    /*
                    VectorLine Line1 = new VectorLine(
                        new RenderPoint(0.5 - alreadywstep, 0.5 - alreadyhstep),
                        new RenderPoint(0.5 + alreadywstep, 0.5 - alreadyhstep));
                    elements.Add(Line1.Split(5));*/

                    if (this._device.UseEllipsoid == true)
                    {
                        for (int k = 0; k < elements.Count; k += 1)
                        {
                            if (elements[k] is IRenderedObject renderedObject)
                                elements[k] = this._device.Ellipsoid.CorrectObject(renderedObject);
                        }
                    }

                }

                this._device.RefreshFrame?.Invoke(elements);
            }
        }
    }

    public class IPAdressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IPAddress address)
            {
                int index = int.Parse(parameter.ToString());
                return address.GetAddressBytes()[index].ToString();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;        
        }
    }
}
