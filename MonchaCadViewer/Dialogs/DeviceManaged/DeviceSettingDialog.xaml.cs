using CadProjectorSDK.Device;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Render;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
                connected.IpAddress = $"{IP1.Text}.{IP2.Text}.{IP3.Text}.{IP4.Text}"; 
            }
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
            this._device.SelectedMesh = null;
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
                var elements = new List<LinesCollection>();

                double widthstep = 1d / (GradientSteps * 2);
                double heightstep = 1d / (GradientSteps * 2);

                LinesCollection dot = new LinesCollection(CadProjectorSDK.Device.Mesh.MeshTypes.NONE);
                dot.Add(new VectorLine(0.5, 0.5, 0.5, 0.5, false));
                elements.Add(dot);

                for (int i = 0; i < GradientCount && this.GradientStep + i <= GradientSteps; i += 1)
                {
                    double alreadywstep = Math.Abs(widthstep * (this.GradientStep + i));
                    double alreadyhstep = Math.Abs(heightstep * (this.GradientStep + i));
                    LinesCollection rect = new LinesCollection(CadProjectorSDK.Device.Mesh.MeshTypes.NONE);

                    /*
                    VectorLine Line2 = new VectorLine(
                            new RenderPoint(0.5 + alreadywstep, 0.5 - alreadyhstep),
                            new RenderPoint(0.5 + alreadywstep, 0.5 + alreadyhstep));
                    elements.Add(Line2);
                                                           
                    VectorLine Line3 = new VectorLine(
                            new RenderPoint(0.5 + alreadywstep, 0.5 + alreadyhstep),
                            new RenderPoint(0.5 - alreadywstep, 0.5 + alreadyhstep));
                    elements.Add(Line3); */


                    VectorLine Line4 = new VectorLine(
                            new RenderPoint(0.5 - alreadywstep, 0),
                            new RenderPoint(0.5 - alreadywstep, 1));
                    rect.Add(Line4);
                    /*
                    VectorLine Line1 = new VectorLine(
                        new RenderPoint(0.5 - alreadywstep, 0.5 - alreadyhstep),
                        new RenderPoint(0.5 + alreadywstep, 0.5 - alreadyhstep));
                    elements.Add(Line1);*/
                    elements.Add(rect);
                }

                // this._device.RefreshFrame?.Invoke(elements);

                /*
                IldaWriter ildaWriter = new IldaWriter();
                if (this._device.Optimized == true && elements.Count > 0)
                {
                    //vectorLines = VectorLinesCollection.Optimize(vectorLines);
                    elements = GraphExtensions.FindShortestCollection(
                        elements, this._device.ProjectionSetting.PathFindDeep, this._device.ProjectionSetting.FindSolidElement);
                }
                var vectorLine = GraphExtensions.GetVectorLines(elements);

                ildaWriter.Write("test.ild",
                    new List<LFrame>() { LFrameConverter.SolidLFrame(vectorLine, this._device) ?? new LFrame() }, 5);*/
            }
        }
    }

    public class IPAdressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string address)
            {
                int index = int.Parse(parameter.ToString());
                return address.Split('.')[index].ToString();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;        
        }
    }
}
