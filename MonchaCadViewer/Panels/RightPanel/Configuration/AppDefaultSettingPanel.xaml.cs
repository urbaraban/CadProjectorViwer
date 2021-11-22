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
using System.Windows.Navigation;
using System.Windows.Shapes;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels.RightPanel.Configuration
{
    /// <summary>
    /// Логика взаимодействия для AppDefaultSettingPanel.xaml
    /// </summary>
    public partial class AppDefaultSettingPanel : UserControl
    {
        public double SizeX
        {
            get => AppSt.Default.default_scale_x;
            set
            {
                AppSt.Default.default_scale_x = value;
                AppSt.Default.Save();
            }
        }

        public double SizeY
        {
            get => AppSt.Default.default_scale_y;
            set
            {
                AppSt.Default.default_scale_y = value;
                AppSt.Default.Save();
            }
        }

        public double Angle
        {
            get => AppSt.Default.default_scale_y;
            set
            {
                AppSt.Default.default_angle = value;
                AppSt.Default.Save();
            }
        }

        public bool Mirror
        {
            get => AppSt.Default.default_mirror;
            set
            {
                AppSt.Default.default_mirror = value;
                AppSt.Default.Save();
            }
        }

        public bool PercentFlag
        {
            get => AppSt.Default.stg_scale_percent;
            set
            {
                AppSt.Default.stg_scale_percent = value;
                AppSt.Default.Save();
            }
        }

        public bool InvertScaleFlag
        {
            get => AppSt.Default.stg_scale_invert;
            set
            {
                AppSt.Default.stg_scale_invert = value;
                AppSt.Default.Save();
            }
        }

        public bool SolidFlag
        {
            get => AppSt.Default.object_solid;
            set
            {
                AppSt.Default.object_solid = value;
                AppSt.Default.Save();
            }
        }

        public bool ShowNameFlag
        {
            get => AppSt.Default.stg_show_name;
            set
            {
                AppSt.Default.stg_show_name = value;
                AppSt.Default.Save();
            }
        }

        public bool SelectableShowFlag
        {
            get => AppSt.Default.stg_selectable_show;
            set
            {
                AppSt.Default.stg_selectable_show = value;
                AppSt.Default.Save();
            }
        }

        public string Attach
        {
            get => AppSt.Default.Attach;
            set
            {
                AppSt.Default.Attach = value;
                OnPropertyChanged("Attach");
                AppSt.Default.Save();
            }
        }

        public double ThinkessMult
        {
            get => AppSt.Default.default_thinkess_percent;
            set
            {
                AppSt.Default.default_thinkess_percent = value;
            }
        }

        public double AnchorSize
        {
            get => AppSt.Default.anchor_size;
            set
            {
                AppSt.Default.anchor_size = value;
            }
        }

        public bool Udp_auto_start
        {
            get => AppSt.Default.udp_auto_run;
            set => AppSt.Default.udp_auto_run = value;
        }

        public AppDefaultSettingPanel()
        {
            InitializeComponent();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "DeviceFrame")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }

    public class AppSettingSave : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value == true ? Brushes.YellowGreen : Brushes.Yellow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class AttachConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value == (string)parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value == true ? parameter : AppSt.Default.Attach;
        }
    }
}
