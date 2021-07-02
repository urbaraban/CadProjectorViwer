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
using ToGeometryConverter.Object;

namespace MonchaCadViewer.Panels
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

        public void Add(bool Clear, GCCollection Objects, string Filepath, bool show = true)
        {
            if (Clear)
            {
                Dispatcher.Invoke(() =>
                {
                    FrameStack.Children.Clear();
                });
            }

            foreach (ScrollPanelItem panelItem in this.FrameStack.Children)
            {
                if (panelItem.FileName == Filepath.Split('\\').Last())
                {
                    if (panelItem.DataContext is CadObject cadObject)
                    {
                        panelItem.DataContext = new CadObjectsGroup(Objects, Filepath.Split('\\').Last(), cadObject.TransformGroup);
                        if (panelItem.IsSelected == true && Objects.Count > 0)
                        {
                            SelectedFrame?.Invoke(panelItem.cadObject, true);
                        }
                        else
                        {
                            panelItem.IsSelected = false;
                        }
                        return;
                    }
                }
            }

            ScrollPanelItem scrollPanelItem = new ScrollPanelItem(new CadObjectsGroup(Objects, Filepath.Split('\\').Last(), null), Filepath);
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
                scrollPanelItem.cadObject.Remove();

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

                SelectedFrame?.Invoke(scrollPanelItem.cadObject, e);
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
