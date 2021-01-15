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

namespace MonchaCadViewer.ToolsPanel.CanvasPanel
{
    /// <summary>
    /// Логика взаимодействия для CadCanvasPanel.xaml
    /// </summary>
    public partial class CadCanvasPanel : UserControl
    {
        public event EventHandler<String> Logging;
        public event EventHandler<CadObject> SelectedObject;

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
            else
            {
                this.Canvas.Clear();
            }

            SelectedObject?.Invoke(this, null);
            UpdateProjection(true);
        }

        public void Clear()
        {
            this.Canvas.Clear();
            SelectedObject?.Invoke(this, null);
        }

        public void DrawRectangle(LPoint3D point1, LPoint3D point2)
        {
            CadDot cadDot1 = new CadDot(point1, MonchaHub.GetThinkess() * 3, true, true, true);
            cadDot1.Render = false;
            CadDot cadDot2 = new CadDot(point2, MonchaHub.GetThinkess() * 3, true, true, true);
            cadDot2.Render = false;
            this.Canvas.Add(cadDot1);
            this.Canvas.Add(cadDot2);
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

        public void DrawMesh(MonchaDeviceMesh mesh, MonchaDevice _device)
        {
            if (_device != null)
            {
                this.Canvas.Clear();

                if (mesh == null)
                    mesh = _device.BaseMesh;

                //
                // Поинты
                //

                for (int i = 0; i < mesh.GetLength(0); i++)
                    for (int j = 0; j < mesh.GetLength(1); j++)
                    {
                        mesh[i, j].M = MonchaHub.Size;

                        CadDot dot = new CadDot(
                             mesh[i, j],
                            this.ActualWidth * 0.02,
                            //calibration flag
                            true, true, false);

                        dot.IsFix = false; // !mesh.OnlyEdge;
                        dot.StrokeThickness = 0;
                        dot.Uid = i.ToString() + ":" + j.ToString();
                        dot.ToolTip = "Позиция: " + i + ":" + j + "\nX: " + mesh[i, j].X + "\n" + "Y: " + mesh[i, j].Y;
                        dot.DataContext = mesh;
                        dot.OnBaseMesh = !mesh.Affine;
                        dot.Render = false;
                        this.Canvas.Add(dot);
                    }

            }
            SelectedObject?.Invoke(null, null);
        }


    }
}
