using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.Dialogs;
using Microsoft.Xaml.Behaviors.Core;
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

        ProjectionScene Scene => (ProjectionScene)this.DataContext;

        public FrameTree()
        {
            InitializeComponent();
        }

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Label label)
            {
                if (label.DataContext is CanvasObject cadObject) cadObject.CadObject.IsSelected = true;
            }
        }

        private void CheckAllBtn_Click(object sender, RoutedEventArgs e)
        {
            RenderStat = !RenderStat;

            if (DataContext is ProjectionScene projectionScene)
            {
                foreach (UidObject cadObject in projectionScene)
                {
                    cadObject.IsRender = RenderStat;
                }
            }
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            AppSt.Default.Save();
        }


       /* public ICommand MakeMaskSplit => new ActionCommand(() => {

            MakeMeshSplitDialog makeMeshSplitDialog = new MakeMeshSplitDialog() { DataContext = this.DataContext };
            makeMeshSplitDialog.ShowDialog();
        });*/

        private void MakeMaskSplit(object sender, RoutedEventArgs e)
        {
            MakeMeshSplitDialog makeMeshSplitDialog = new MakeMeshSplitDialog() { DataContext = this.DataContext };
            makeMeshSplitDialog.ShowDialog();
        }
    }
}
