using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
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
using ToGeometryConverter.Object;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для ScrollPanel.xaml
    /// </summary>
    public partial class ScrollPanel : UserControl
    {
        private ProjectionScene MainScene => (ProjectionScene)this.DataContext;

        public ScrollPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Add object on scroll panel.
        /// </summary>
        /// <param name="Clear">Clear Main scene</param>
        /// <param name="Obj">Add object</param>
        /// <param name="Filepath">Path for refresh</param>
        /// <param name="show">Condition for show object on Main scene</param>
        public void Add(bool Clear, UidObject Obj, string Filepath, bool show = true)
        {
            if (Clear)
            {
                Dispatcher.Invoke(() =>
                {
                    FrameStack.Children.Clear();
                });
            }

            ProjectionScene projectionScene = new ProjectionScene(Obj);

            foreach (ScrollPanelItem panelItem in this.FrameStack.Children)
            {
                if (panelItem.FileName == Filepath.Split('\\').Last())
                {
                    if (panelItem.DataContext is ProjectionScene scene)
                    {
                        panelItem.DataContext = projectionScene; // new CadObjectsGroup(Objects, Filepath.Split('\\').Last(), cadObject.TransformGroup);

                        if (panelItem.IsSelected == true)
                        {
                            panelItem.IsSelected = true;
                        }
                        else
                        {
                            panelItem.IsSelected = false;
                        }
                        return;
                    }
                }
            }

            

            ScrollPanelItem scrollPanelItem = new ScrollPanelItem(Filepath) { DataContext = projectionScene };
            scrollPanelItem.Selected += ScrollPanelItem_Selected;
            scrollPanelItem.Removed += ScrollPanelItem_Removed;
            Dispatcher.Invoke(() =>
            {
                this.FrameStack.Children.Add(scrollPanelItem);
                if (show == true)
                {
                    scrollPanelItem.IsSelected = true;
                }
            });

        }

        private void ScrollPanelItem_Removed(object sender, EventArgs e)
        {
            if (sender is ScrollPanelItem scrollPanelItem)
            {
                MainScene.Remove(scrollPanelItem.Scene);
                FrameStack.Children.Remove(scrollPanelItem);
            }
        }

        private void ScrollPanelItem_Selected(object sender, bool e)
        {
            if (sender is ScrollPanelItem scrollPanelItem)
            {
                if (e == true)
                {
                    if (Keyboard.Modifiers != ModifierKeys.Shift)
                    {
                        foreach (UIElement uIElement in this.FrameStack.Children)
                        {
                            if (uIElement != sender && uIElement is ScrollPanelItem panelItem)
                            {
                                panelItem.IsSelected = false;
                            }
                        }

                        MainScene.Clear();
                    }

                    foreach(UidObject cadObject in scrollPanelItem.Scene.Objects)
                    {
                        if (cadObject is CadGroup group && AppSt.Default.object_solid == false)
                        {
                            MainScene.AddRange(group.Children);
                        }
                        else
                        {
                            MainScene.Add(scrollPanelItem.Scene);
                        }
                    }

                   // MainScene.Add(scrollPanelItem.Scene);
                }
                else
                {
                    MainScene.Remove(scrollPanelItem.Scene);
                }

            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            while(FrameStack.Children.Count > 0)
            {
                if (FrameStack.Children[0] is ScrollPanelItem scrollPanel)
                {
                    scrollPanel.Remove();
                }
            }
        }

        private void AllSolvedBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (ScrollPanelItem scrollPanel in FrameStack.Children)
            {
                scrollPanel.IsSolved = true;
            }
        }

        public void Refresh()
        {
            foreach (ScrollPanelItem scrollPanel in FrameStack.Children)
            {
                scrollPanel.Refresh();
            }
        }
    }
}
