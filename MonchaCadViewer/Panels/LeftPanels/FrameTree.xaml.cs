using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Interfaces;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.Dialogs;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections;
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
                if (label.DataContext is CanvasObject cadObject) cadObject.CadObject.Select(true);
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
        public ICommand MaskSplitCommand => new ActionCommand(() => MakeMaskSplit());
        private void MakeMaskSplit()
        {
            if (this.DataContext is ProjectionScene scene)
            {
                MakeMeshSplitDialog makeMeshSplitDialog = new MakeMeshSplitDialog(scene.Size.Bounds, scene);
                makeMeshSplitDialog.Show();
            }
        }

        public ICommand ClearMasks => new ActionCommand(() => Clear());

        private void Clear()
        {
            if (this.DataContext is ProjectionScene scene)
            {
                for (int i = scene.Masks.Count - 1; i > -1; i -= 1)
                {
                    scene.Masks[i].Remove();
                }
                scene.Masks.Clear();
            }
        }

        private void SendLayer(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ProjectionScene scene)
            {
                byte[] layers = new byte[scene.Count];
                for(int i = 0; i < layers.Length; i += 1)
                {
                    layers[i] = (byte)(i % 2);
                }

                SceneTask sceneTask = new SceneTask()
                {
                    Object = layers,
                    TableID = Scene.TableID,
                    TaskID = -1,
                    TaskInfo = new System.IO.FileInfo("Layers")
                };

                scene.RunTask(sceneTask, false);
            }
        }

        private async void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is IConnected connected)
            {
                await connected.Reconnect();
            }
        }
    }
}
