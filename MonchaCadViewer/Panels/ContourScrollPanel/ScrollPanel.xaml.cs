using CadProjectorSDK;
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
        private ProjectorHub hub => (ProjectorHub)this.DataContext;

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
        private void ScrollPanelItem_Removed(object sender, EventArgs e)
        {
            if (sender is ScrollPanelItem scrollPanelItem)
            {
                hub.ScenesCollection.SelectedScene.Remove();
            }
        }

        private void ScrollPanelItem_Selected(object sender, bool e)
        {
            /*  if (sender is ScrollPanelItem scrollPanelItem)
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

                          hub.Scene.Clear();
                      }

                      foreach(UidObject cadObject in scrollPanelItem.Scene.Objects)
                      {
                          if (cadObject is CadGroup group && AppSt.Default.object_solid == false)
                          {
                              hub.Scene.AddRange(group.Children);
                          }
                          else
                          {
                              hub.Scene.Add(scrollPanelItem.Scene);
                          }
                      }

                     // MainScene.Add(scrollPanelItem.Scene);
                  }
                  else
                  {
                      hub.Scene.Remove(scrollPanelItem.Scene);
                  }

              }  */
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            hub.ScenesCollection.LoadedObject.Clear();
        }

        private void AllSolvedBtn_Click(object sender, RoutedEventArgs e)
        {
           /* foreach (ScrollPanelItem scrollPanel in FrameStack.Children)
            {
                scrollPanel.IsSolved = true;
            }*/
        }

        public void Refresh()
        {
            /*foreach (ScrollPanelItem scrollPanel in FrameStack.Children)
            {
                scrollPanel.Refresh();
            }*/
        }
    }
}
