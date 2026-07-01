using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Device;
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
            if (sender is not Label label)
            {
                return;
            }

            UidObject uidObject = label.DataContext switch
            {
                CanvasObject canvasObject => canvasObject.CadObject,
                UidObject obj => obj,
                _ => null
            };

            if (uidObject == null)
            {
                return;
            }

            if (DataContext is ProjectionScene scene && scene.StepByStep)
            {
                ApplyStepByStepRender(scene, uidObject);
                return;
            }

            uidObject.Select(true);
        }

        private static void ApplyStepByStepRender(ProjectionScene scene, UidObject target)
        {
            UidObject renderTarget = ResolveRenderTarget(target);

            foreach (UidObject obj in scene)
            {
                if (obj is CadGroup)
                {
                    SetRenderRecursive(obj, false);
                }
                else if (obj.Uid != renderTarget.Uid)
                {
                    obj.SetRender(false, false);
                }
                else
                {
                    obj.SetRender(false, true);
                }
            }

            renderTarget.SetRender(true, false);

            SelectStepObject(scene, target);
        }

        private static UidObject ResolveRenderTarget(UidObject target)
        {
            if (target is CadGroup group && group.Count > 0)
            {
                return group[0];
            }

            return target;
        }

        private static void SetRenderRecursive(UidObject obj, bool render)
        {
            if (obj is CadGroup group)
            {
                foreach (UidObject child in group)
                {
                    SetRenderRecursive(child, render);
                }
            }
            else
            {
                obj.SetRender(render, true);
            }
        }

        private static void SelectStepObject(ProjectionScene scene, UidObject uidObject)
        {
            if (scene.SelectedObjects.Contains(uidObject))
            {
                return;
            }

            scene.SelectedObjects.Clear();
            scene.SelectedObjects.Add(uidObject);
        }

        private void CheckAllBtn_Click(object sender, RoutedEventArgs e)
        {
            RenderStat = !RenderStat;

            if (DataContext is ProjectionScene projectionScene)
            {
                foreach (UidObject cadObject in projectionScene)
                {
                    cadObject.SetRender(RenderStat, false);
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

        public ICommand RefreshAllDevicesCommand => new ActionCommand(async () => {
            if (this.DataContext is ProjectionScene scene)
            {
                foreach (LProjector lProjector in scene.Projectors)
                {
                    if (lProjector is IConnectebleDevice connected)
                    {
                        await connected.Reconnect();
                    }
                }
            }
        });

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


        private async void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is IConnectebleDevice connected)
            {
                await connected.Reconnect();
            }
        }
    }
}
