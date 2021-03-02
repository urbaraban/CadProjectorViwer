using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
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
using System.Windows.Shapes;

namespace MonchaCadViewer.ToolsPanel.ObjectPanel
{
    /// <summary>
    /// Логика взаимодействия для CadRectangleSizePanel.xaml
    /// </summary>
    public partial class CadRectangleSizePanel : Window
    {
        public CadRectangleSizePanel(CadRectangle  cadRectangle)
        {
            InitializeComponent();
            WidthUpDn.DataContext = cadRectangle.LRect;
            WidthUpDn.SetBinding(NumericUpDown.ValueProperty, "Width");
            HeightUpDn.DataContext = cadRectangle.LRect;
            HeightUpDn.SetBinding(NumericUpDown.ValueProperty, "Height");
        }

    }
}
