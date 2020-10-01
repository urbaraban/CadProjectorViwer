using MonchaSDK;
using MonchaSDK.Device;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MonchaSDK.Object;
using System.Windows.Shapes;

namespace MonchaCadViewer.CanvasObj
{
    public class CadCanvas : Canvas
    {
        public event EventHandler<CadObject> SelectedObject;
        //public event EventHandler<string> ErrorMessageEvent;

        private LPoint3D _size;
        private List<CadDot> anchors = new List<CadDot>();
        private int _status = 0;
        private bool _maincanvas;

        public bool InverseStep { get; set; } = true;
        public bool HorizontalMesh { get; set; } = false;


        public int Status
        {
            get => _status;
            set => _status = value;
        }

        public CadCanvas(LPoint3D Size, bool MainCanvas)
        {
            this._size = Size;
            this._maincanvas = MainCanvas;

            this.Name = "CCanvas";
            this.Background = Brushes.Transparent; //backBrush;
            this.Width = this._size.GetMPoint.X;
            this.Height = this._size.GetMPoint.Y;


            this._size.ChangePoint += _size_ChangePoint;
            this._size.M.ChangePoint += _size_ChangePoint;

            this.Focusable = true;
            this.KeyUp += Canvas_KeyUp;
            if (this._maincanvas)
            {
                this.MouseLeftButtonDown += Canvas_MouseLeftDown;
                MonchaHub.NeedUpdateFrame += MonchaHub_NeedUpdateFrame;
            }

            MonchaHub.ChangeSize += MonchaHub_ChangeSize;

        }

        public void UpdateProjection()
        {
            SendProcessor.Worker(this);
        }
        private void MonchaHub_NeedUpdateFrame(object sender, bool e)
        {
            Console.WriteLine("NeedUpdate");
            if (this._maincanvas)
                SendProcessor.Worker(this);
        }

        private void MonchaHub_ChangeSize(object sender, LPoint3D e)
        {
            ResizeCanvas();
        }

        private void _size_ChangePoint(object sender, LPoint3D e)
        {
            ResizeCanvas();
        }

        public void ResizeCanvas()
        {
            if (this._size.X != 0 && this._size.Y != 0 && this._size.Z != 0 && this._size.M.X != 0)
            {
                if (this.Parent is Viewbox viewbox)
                {
                    viewbox.Width = this._size.GetMPoint.X;
                    viewbox.Height = this._size.GetMPoint.Y;
                }
                this.Width = this._size.GetMPoint.X;
                this.Height = this._size.GetMPoint.Y;
            }
        }

        public void DrawContour(Shape obj, bool maincanvas, bool add, bool mousemove)
        {

            foreach (MonchaDevice device in MonchaHub.Devices)
            {
                device.Calibration = false;
            }

            if (add == false)
            {
                this.Clear();
            }

            if (obj is CadObject cadObject)
            {
                cadObject.Updated += Object_Updated;
            }

            this.Add(obj);

            if (this._maincanvas)
                SendProcessor.Worker(this);
        }

        private void Object_Updated(object sender, CadObject e)
        {
            if (this._maincanvas)
                SendProcessor.Worker(this);
        }

        private void Obj_Updated(object sender, CadObject e)
        {
            if (this._maincanvas)
                SendProcessor.Worker(this);
        }

        public void DrawRectangle(LPoint3D point1, LPoint3D point2)
        {
            CadDot cadDot1 = new CadDot(point1, MonchaHub.GetThinkess() * 3, true, false);
            cadDot1.Render = false;
            CadDot cadDot2 = new CadDot(point2, MonchaHub.GetThinkess() * 3, true, false);
            cadDot2.Render = false;
            this.Children.Add(cadDot1);
            this.Children.Add(cadDot2);
        }



        private void Canvas_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            switch (_status)
            {
                case 1:

                    break;
            }
        }

        public CadDot UndrMouseAnchor(Point point, CadDot selectDot)
        {
            foreach (CadDot dot in anchors)
                if (dot.Contains(point))
                    if (dot != selectDot)
                        return dot;

            return null;
        }


        private void Canvas_KeyUp(object sender, KeyEventArgs e)
        {

        }


        public CadDot[,] GetMeshDot(int Height, int Width)
        {
            CadDot[,] mesh = new CadDot[Height, Width];
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    mesh[i, j] = this.Children[i * Width + j] as CadDot;
                }
            return mesh;
        }

        //Рисуем квадраты в поле согласно схеме
        public void DrawMesh(MonchaDeviceMesh mesh, MonchaDevice _device)
        {
            if (_device != null)
            {
                _device.Calibration = !mesh.Affine;
                this.DataContext = _device;
                this.Children.Clear();

                if (mesh == null)
                    mesh = _device.BaseMesh;

                //
                // Поинты
                //

                for (int i = 0; i < mesh.GetLength(0); i++)
                    for (int j = 0; j < mesh.GetLength(1); j++)
                    {
                        mesh[i, j].M = this._size;

                        CadDot dot = new CadDot(
                             mesh[i, j],
                            this.ActualWidth * 0.02,
                            //calibration flag
                            true, false);

                        dot.IsFix = false; // !mesh.OnlyEdge;
                        dot.StrokeThickness = 0;
                        dot.Uid = i.ToString() + ":" + j.ToString();
                        dot.ToolTip = "Позиция: " + i + ":" + j + "\nX: " + mesh[i, j].X + "\n" + "Y: " + mesh[i, j].Y;
                        dot.DataContext = mesh;
                        dot.OnBaseMesh = !mesh.Affine;
                        dot.Render = false;
                        dot.Updated += Object_Updated;
                        this.Add(dot);
                    }

            }
        }

        public void Clear()
        {
            foreach (CadObject cadObject in this.Children)
            {
                cadObject.Updated -= Obj_Updated;
                cadObject.Selected -= Obj_Selected;
            }
            SelectedObject?.Invoke(this, null);
            this.Children.Clear();
        }

        public void Add(Shape obj)
        {
            if (this.Children.Equals(obj) == false)
            {
                if (obj is CadObject cadObject)
                {
                    cadObject.Updated += Obj_Updated;
                    cadObject.Selected += Obj_Selected;
                }
                this.Children.Add(obj);
            }

        }

        private void Obj_Selected(object sender, CadObject e)
        {
            this.SelectedObject?.Invoke(this, e);
        }
    }
}
