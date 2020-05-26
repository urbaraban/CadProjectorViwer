using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MonchaCadViewer.CanvasObj
{
    public class CadCanvas : Canvas
    {
        static Dispatcher dispatcher = System.Windows.Application.Current.Dispatcher;

        public event EventHandler<CadObject> SelectedObject;

        private bool CalibrationStat = false;
        private MonchaPoint3D _size;
        private List<CadDot> anchors = new List<CadDot>();
        private int _status = 0;

        public bool MorphMesh { get; set; } = true;
        public bool HorizontalMesh { get; set; } = false;


        public int Status
        {
            get => _status;
            set => _status = value;
        }

        public CadCanvas(MonchaPoint3D Size)
        {
            this._size = Size;
            
            this.Name = "CCanvas";
            this.Background = Brushes.Transparent; //backBrush;
            this.Width = this._size.GetMPoint.X;
            this.Height = this._size.GetMPoint.Y;
            this.ClipToBounds = true;


            this._size.ChangePoint += _size_ChangePoint;
            this._size.M.ChangePoint += _size_ChangePoint;

            this.Focusable = true;
            
            this.KeyUp += Canvas_KeyUp;
            this.MouseLeftButtonDown += Canvas_MouseLeftDown;

        }

        private void _size_ChangePoint(object sender, MonchaPoint3D e)
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

        public void DrawContour(LObjectList _innerList, bool maincanvas, bool add, bool mousemove)
        {
            if (_innerList.Count > 0)
            {
                foreach (MonchaDevice device in MonchaHub.Devices)
                {
                    device.Calibration = false;
                }

                if (!add)
                {
                    this.Children.Clear();
                }

                MonchaPoint3D Center = new MonchaPoint3D(0.5, 0.5, 0);
                Center.M = this._size;

                CadContour polygon = new CadContour(_innerList, Center, maincanvas, mousemove);
                polygon.OnBaseMesh = false;
                this.Children.Add(polygon);

            }
        }

        public void SubsObj (CadObject obj)
        {
            obj.Updated += Obj_Updated;
        }

        private void Obj_Updated(object sender, CadObject e)
        {
            SendProcessor.Worker(this);
        }

        public void DrawRectangle(MonchaPoint3D point1, MonchaPoint3D point2)
        {
            CadRectangle cadRectangle = new CadRectangle(true, point1, point2, false);
            CadDot cadDot1 = new CadDot(point1, 50, false, true, false);
            CadDot cadDot2 = new CadDot(point2, 50, false, true, false);

            this.Children.Add(cadRectangle);
            this.Children.Add(cadDot1);
            this.Children.Add(cadDot2);
        }

        public void DrawLine()
        {
            CadDot newdot1 = this.UndrMouseAnchor(Mouse.GetPosition(this), null);
            if (newdot1 == null)
            {
                newdot1 = NewAnchor(false, true, false);
                newdot1.WasMove = true;
                this.Children.Add(newdot1);
            }

            CadDot newdot2 = NewAnchor(false, true, true);
            this.Children.Add(newdot2);

            CadLine lineSbcr = new CadLine(newdot1.BaseContextPoint, newdot2.BaseContextPoint, true);

            this.Children.Add(lineSbcr);
        }


        private void Canvas_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            switch (_status)
            {
                case 1:
                    DrawLine();
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

        public void RemoveAnchor(CadDot anchor)
        {
            this.Children.Remove(anchor);
            anchors.Remove(anchor);
        }

        private CadDot NewAnchor(bool Calibration, bool mousemove, bool move )
        {
            Point point = Mouse.GetPosition(this);
            CadDot anchor = new CadDot(new MonchaPoint3D(point.X, point.Y, 0), this.ActualWidth * 0.02, false, mousemove, move);
            anchors.Add(anchor);
            return anchor;
        }


        private void Canvas_KeyUp(object sender, KeyEventArgs e)
        {

        }

        public async void UnselectAll(CadObject SelectObj = null)
        {
            try
            {
                foreach (CadObject cadObject in this.Children)
                    await Task.Run(() =>
                    {
                        if (cadObject != SelectObj)
                        {
                            cadObject.IsSelected = false;
                        }
                    });
            }
            catch (Exception e) 
            {
                Console.WriteLine(e.Message);
            };

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
        public void DrawMesh(MonchaDeviceMesh mesh, MonchaDevice _device, bool calibration, bool OnBaseMesh, bool Render)
        {
            if (_device != null)
            {
                _device.Calibration = calibration;
                this.DataContext = _device;
                this.Children.Clear();

                if (mesh == null)
                    mesh = _device.BaseMesh;

                this.CalibrationStat = calibration;
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
                            //multiplier

                            //calibration flag
                            true,
                            true,
                            false);

                        dot.Fill = Brushes.Black;
                        dot.StrokeThickness = 0;
                        dot.Uid = i.ToString() + ":" + j.ToString();
                        dot.ToolTip = "Позиция: " + i + ":" + j + "\nX: " + mesh[i, j].X + "\n" + "Y: " + mesh[i, j].Y;
                        dot.DataContext = mesh;
                        dot.OnBaseMesh = OnBaseMesh;
                        dot.Render = Render;
                        this.Children.Add(dot);
                    }

            }
        }
    }
}
