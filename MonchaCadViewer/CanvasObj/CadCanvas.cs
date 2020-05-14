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
        private double _size;
        private object actualanchor;
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
            this.MouseMove += CadCanvas_MouseMove;
            this.PreviewKeyUp += CadCanvas_PreviewKeyUp;

            this.MouseLeftButtonDown += Canvas_MouseLeftDown;
            this.MouseLeftButtonUp += Canvas_MouseLeftUp;
        }

        private void CadCanvas_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }

        private void CadCanvas_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void Canvas_MouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void Canvas_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            switch (_status)
            {
                case 1:
                    DotShape newdot1 = (DotShape)actualanchor;
                    if (newdot1 == null)
                    {
                        newdot1 = NewAnchor(e.GetPosition(this), (int)(this.ActualWidth * 0.02), new MonchaPoint3D(1, 1, 1), false);
                        newdot1.WasMove = true;
                        this.Children.Add(newdot1);
                    }

                    DotShape newdot2 = NewAnchor(e.GetPosition(this), (int)(this.ActualWidth * 0.02), new MonchaPoint3D(1, 1, 1), false);
                    this.Children.Add(newdot2);

                    LineSbcr lineSbcr = new LineSbcr(newdot1.BaseContextPoint, newdot2.BaseContextPoint);
                    lineSbcr.Edit += LineSbcr_Edit;
                    this.Children.Add(lineSbcr);

                    break;
            }
        }

        private void LineSbcr_Edit(object sender, bool e)
        {
            if (sender is LineSbcr line && this.actualanchor is DotShape dot)
                line.SecondContextPoint = dot.BaseContextPoint;
        }

        private DotShape NewAnchor(Point point, int size, MonchaPoint3D Mult, bool Calibration)
        {
            DotShape newdot = new DotShape(point, this.ActualWidth * 0.02, new MonchaPoint3D(1, 1, 1), false);
            return newdot;
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
    }
}
