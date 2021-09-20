using CadProjectorViewer.CanvasObj;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels.DevicePanel.LeftPanels
{
    /// <summary>
    /// Логика взаимодействия для FrameTree.xaml
    /// </summary>
    public partial class FrameTree : UserControl
    {
        private bool RenderStat = true;

        public FrameTree()
        {
            InitializeComponent();
        }

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Label label)
            {
                if (label.DataContext is CadObject cadObject) cadObject.IsSelected = true;
            }
        }

        private void CheckAllBtn_Click(object sender, RoutedEventArgs e)
        {
            RenderStat = !RenderStat;

            if (DataContext is ProjectionScene projectionScene)
            {
                foreach (CadObject cadObject in projectionScene.Objects)
                {
                    cadObject.Render = RenderStat;
                }
            }
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            AppSt.Default.Save();
        }
    }
}
