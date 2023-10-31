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

        public double ThinkessMult
        {
            get => AppSt.Default.default_thinkess_percent;
            set
            {
                AppSt.Default.default_thinkess_percent = value;
                AppSt.Default.Save();
            }
        }

        public double AnchorSize
        {
            get => AppSt.Default.anchor_size;
            set
            {
                AppSt.Default.anchor_size = value;
                AppSt.Default.Save();
            }
        }

        public bool Udp_auto_start
        {
            get => AppSt.Default.udp_auto_run;
            set
            {
                AppSt.Default.udp_auto_run = value;
                AppSt.Default.Save();
            }
        }

        public bool App_kill_other_process
        {
            get => AppSt.Default.app_auto_kill_other_process;
            set
            {
                AppSt.Default.app_auto_kill_other_process = value;
            }
        }

        public bool Show_hide_object
        {
            get => AppSt.Default.show_hide_object;
            set
            {
                AppSt.Default.show_hide_object = value;
                AppSt.Default.Save();
            }
        }


        public AppDefaultSettingPanel()
        {
            InitializeComponent();
        }

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
}
