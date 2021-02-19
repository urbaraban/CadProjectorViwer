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
using System.Windows.Navigation;

namespace MonchaCadViewer.ToolsPanel.ContourScrollPanel
{
    /// <summary>
    /// Логика взаимодействия для ScrollPanel.xaml
    /// </summary>
    public partial class ScrollPanel : UserControl
    {
        public event EventHandler<bool> SelectedFrame;

        public ScrollPanel()
        {
            InitializeComponent();
        }

        public void Add(bool Clear, GeometryGroup geometry, string Name, bool show = true)
        {
            if (Clear)
            {
                FrameStack.Children.Clear();
            }
            CadObjectsGroup cadObjectsGroup = new CadObjectsGroup(geometry, Name);
 
            ScrollPanelItem scrollPanelItem = new ScrollPanelItem(cadObjectsGroup);
            scrollPanelItem.Selected += ScrollPanelItem_Selected;
            scrollPanelItem.Removed += ScrollPanelItem_Removed;
            this.FrameStack.Children.Add(scrollPanelItem);

            if (show == true)
            {
                scrollPanelItem.IsSelected = true;
            }
        }

        private void ScrollPanelItem_Removed(object sender, EventArgs e)
        {
            if (sender is ScrollPanelItem scrollPanelItem)
            {
                foreach (CadObject cadObject in scrollPanelItem.objectsGroup)
                {
                    cadObject.Remove();
                }

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
                    }
                }

                SelectedFrame?.Invoke(scrollPanelItem.objectsGroup, e);
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
    }
}
