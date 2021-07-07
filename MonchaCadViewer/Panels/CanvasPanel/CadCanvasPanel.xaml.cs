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
using AppSt = MonchaCadViewer.Properties.Settings;

namespace MonchaCadViewer.Panels.CanvasPanel
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

            this.Canvas.MouseMove += Canvas_MouseMove;
            this.DataContextChanged += CadCanvasPanel_DataContextChanged;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point tempPoint = e.GetPosition(Canvas);
            CoordinateLabel.Content = $"X: { Math.Round(tempPoint.X, 2) }; Y:{ Math.Round(tempPoint.Y, 2) }";
        }

        private void Canvas_UpdateProjection(object sender, EventArgs e)
        {
            UpdateProjection(true);
        }

        private void CadCanvasPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is CadObjectsGroup objectsGroup)
            {
                if (AppSt.Default.object_solid == false)
                {
                    foreach (CadObject cadObject in objectsGroup)
                    {
                        cadObject.ShowName = false;
                        this.Canvas.DrawContour(cadObject, Keyboard.Modifiers != ModifierKeys.Shift);
                    }
                }
                else this.Canvas.DrawContour(objectsGroup, Keyboard.Modifiers != ModifierKeys.Shift);
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

        internal void Remove(object sender)
        {
            if (sender is CadObjectsGroup cadGeometries)
            {
                foreach (CadObject cadObject in cadGeometries)
                {
                    cadObject.Remove();
                }
            } 
                
            else
                this.Canvas.RemoveChildren((FrameworkElement)sender);
        }

        internal void Add(object sender, bool Clear) => this.Canvas.DrawContour((CadObject)sender, Clear);

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double cell = (int)(Math.Min(MonchaHub.Size.X, MonchaHub.Size.Y) / 10);
            CanvasBrush.Viewport = new Rect(0, 0, cell, cell);
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

        private void ShowDeviceRect_Click(object sender, RoutedEventArgs e)
        {
            foreach(MonchaDevice monchaDevice in MonchaHub.Devices)
            {
                this.Canvas.DrawContour(new CadRectangle(monchaDevice.Size, monchaDevice.HWIdentifier, false), false);
                foreach (LDeviceMesh mesh in monchaDevice.SelectedMeshes)
                {
                    this.Canvas.DrawContour(new CadRectangle(mesh.Size, mesh.Name, false), false);
                }
            }
        }
    }
}
