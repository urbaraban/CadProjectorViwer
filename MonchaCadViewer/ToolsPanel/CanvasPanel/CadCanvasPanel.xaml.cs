using MonchaCadViewer.CanvasObj;
using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
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
using AppSt = MonchaCadViewer.Properties.Settings;

namespace MonchaCadViewer.ToolsPanel.CanvasPanel
{
    /// <summary>
    /// Логика взаимодействия для CadCanvasPanel.xaml
    /// </summary>
    public partial class CadCanvasPanel : UserControl
    {
        private Visibility _showadorner = Visibility.Hidden;

        public event EventHandler<String> Logging;
        public event EventHandler<CadObject> SelectedObject;

        /// <summary>
        /// Orientation flag for SelecNext void
        /// </summary>
        public bool InverseSelectFlag = false;

        public CadCanvasPanel()
        {
            InitializeComponent();
            this.Canvas.SelectedObject += CadCanvas_SelectedObject;
            this.Canvas.UpdateProjection += Canvas_UpdateProjection;
            this.DataContextChanged += CadCanvasPanel_DataContextChanged;
        }

        private void Canvas_UpdateProjection(object sender, EventArgs e)
        {
            UpdateProjection(true);
        }

        private void CadCanvasPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Shift)
            {
                this.Canvas.Clear();
            }

            if (this.DataContext is CadObjectsGroup objectsGroup)
            {
                foreach (CadObject cadObject in objectsGroup.Objects)
                {
                    this.Canvas.DrawContour(cadObject, true, true);
                }
            }


            SelectedObject?.Invoke(this, null);
            UpdateProjection(true);
        }

        public void Clear()
        {
            this.Canvas.Clear();
            SelectedObject?.Invoke(this, null);
        }



        private void CadCanvas_SelectedObject(object sender, CadObject e)
        {
            SelectedObject?.Invoke(this, e);
        }

        public void UpdateProjection(bool force)
        {
            SendProcessor.Worker(Canvas);
        }

        public void MoveCanvasSet(double left, double top)
        {
            for (int i = 0; i < this.Canvas.Children.Count; i++)
            {
                if (this.Canvas.Children[i] is CadObject cadObject)
                {
                    if (cadObject.IsSelected == true && cadObject.IsFix == false)
                    {
                        cadObject.X += left;
                        cadObject.Y += top;
                    }
                }
            }
        }

        public void FixPosition()
        {
            for (int i = 0; i < Canvas.Children.Count; i++)
            {
                if (Canvas.Children[i] is CadObject cadObject1)
                {
                    if (cadObject1.IsSelected)
                    {
                        cadObject1.IsFix = !cadObject1.IsFix;
                    }
                }
            }

        }

        public void SelectNext()
        {
            for (int i = 0; i < Canvas.Children.Count; i++)
            {
                if (Canvas.Children[i] is CadObject cadObject)
                {
                    if (cadObject.IsSelected)
                    {
                        cadObject.IsFix = true;
                        cadObject.IsSelected = false;

                        try
                        {
                            if (Canvas.Children[i + (InverseSelectFlag ? -1 : +1)] is CadObject cadObject2)
                            {
                                cadObject2.IsSelected = true;
                                cadObject2.IsFix = false;
                            }
                        }
                        catch (Exception exeption)
                        {
                            Logging?.Invoke(this, ($"Main: {exeption.Message}"));
                            InverseSelectFlag = !InverseSelectFlag;
                            cadObject.IsSelected = true;
                        }
                        break;
                    }
                }
            }

        }

        

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CanvasBrush.Viewport = new Rect(0, 0, 200 * MonchaHub.Size.M.X, 200 * MonchaHub.Size.M.X);
        }

        private void AdornerShowBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (UIElement uIElement in this.Canvas.Children)
            {
                if (uIElement is CadObject cadObject)
                {
                    if (cadObject.ObjAdorner != null)
                    {
                        cadObject.ObjAdorner.IsEnabled = true;
                    }
                }
            }
        }
    }
}
