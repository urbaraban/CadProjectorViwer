using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MonchaCadViewer.CanvasObj
{
    public class CadCanvas : Canvas
    {
        private bool CalibrationStat = false;
        private double _size;
        private List<DotShape> anchors = new List<DotShape>();
        private int _status = 0;

        public bool MorphMesh { get; set; } = true;
        public bool HorizontalMesh { get; set; } = false;

        public int Status
        {
            get => _status;
            set => _status = value;
        }

        public CadCanvas(double Width, double Height)
        {
            this._size = Math.Max(Width, Height);
            Rect rect = new Rect(0, 0, 500, 500);
            RectangleGeometry grect = new RectangleGeometry(rect, 0, 0);

            GeometryDrawing geometryDrawing = new GeometryDrawing();
            geometryDrawing.Geometry = grect;

            geometryDrawing.Pen = new Pen(Brushes.LightGray, 1);

            DrawingBrush backBrush = new DrawingBrush(geometryDrawing);
            backBrush.TileMode = TileMode.None;
            backBrush.Viewport = new Rect(0, 0, 500, 500); 
            //backBrush.ViewboxUnits = BrushMappingMode.Absolute;

            this.Name = "CCanvas";
            this.Background = Brushes.Transparent; //backBrush;
            this.Width = Width;
            this.Height = Height;

            this.Focusable = true;
            
            this.KeyUp += Canvas_KeyUp;
            this.MouseLeftButtonDown += Canvas_MouseLeftDown;

        }

        public void DrawOnCanvas(LObjectList _innerList, bool maincanvas, bool add, bool mousemove)
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

                ViewContour polygon = new ViewContour(_innerList.GetOnlyPoints, new MonchaPoint3D(this.ActualWidth / 2, this.ActualHeight / 2, 0), maincanvas, mousemove);
                polygon.OnBaseMesh = false;
                this.Children.Add(polygon);

            }
        }


        private void Canvas_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            switch (_status)
            {
                case 1:
                    DotShape newdot1 = this.UndrMouseAnchor(Mouse.GetPosition(this), null);
                    if (newdot1 == null)
                    {
                        newdot1 = NewAnchor(false, true, false);
                        newdot1.WasMove = true;
                        this.Children.Add(newdot1);
                    }

                    DotShape newdot2 = NewAnchor(false, true, true);
                    this.Children.Add(newdot2);

                    LineSbcr lineSbcr = new LineSbcr(newdot1.BaseContextPoint, newdot2.BaseContextPoint, true);

                    this.Children.Add(lineSbcr);

                    break;
            }
        }

        public DotShape UndrMouseAnchor(Point point, DotShape selectDot)
        {
            foreach (DotShape dot in anchors)
                if (dot.CheckInArea(point))
                    if (dot != selectDot)
                        return dot;

            return null;
        }

        public void RemoveAnchor(DotShape anchor)
        {
            this.Children.Remove(anchor);
            anchors.Remove(anchor);
        }

        private DotShape NewAnchor(bool Calibration, bool mousemove, bool move )
        {
            DotShape anchor = new DotShape(Mouse.GetPosition(this), this.ActualWidth * 0.02, new MonchaPoint3D(1, 1, 1), false, mousemove, move);
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
            catch { };

        }

        public DotShape[,] GetMeshDot(int Height, int Width)
        {
            DotShape[,] mesh = new DotShape[Height, Width];
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    mesh[i, j] = this.Children[i * Width + j] as DotShape;
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
                        //invert point on Y
                        DotShape dot = new DotShape(
                             mesh[i, j].GetPoint,
                            this.ActualWidth * 0.02,
                            //multiplier
                            new MonchaPoint3D(this.ActualWidth, this.ActualHeight, 0),
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
