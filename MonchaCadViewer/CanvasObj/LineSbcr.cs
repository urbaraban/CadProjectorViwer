using MonchaCadViewer.CanvasObj.DimObj;
using MonchaSDK.Object;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MonchaCadViewer.CanvasObj
{
    class LineSbcr : CadObject
    {
        private LineGeometry _lineGeometry;
        private double _lenth;

        private AdornerLayer adornerLayer;

        protected override Geometry DefiningGeometry => _lineGeometry;

        public event EventHandler<bool> Edit;
        public event EventHandler<MonchaPoint3D> ChangePoint0;
        public event EventHandler<MonchaPoint3D> ChangePoint1;


        public object SecondContextPoint { get; set; }

        public MonchaPoint3D SPoint {
            get => SecondContextPoint as MonchaPoint3D;
            }

        public LineSbcr(object StartPoint, object EndPoint)
        {
            this.BaseContextPoint = StartPoint;
            if (this.BaseContextPoint is MonchaPoint3D basepoint)
            {
                basepoint.ChangePoint += Basepoint_ChangePoint;
            }
            this.SecondContextPoint = EndPoint;

            if (this.SecondContextPoint is MonchaPoint3D secondpoint)
            {
                secondpoint.ChangePoint += Basepoint_ChangePoint;
            }

            this.Stroke = Brushes.Red;
            this.StrokeThickness = 40;
            this._lineGeometry = new LineGeometry();

            ContextMenuLib.LineContextMenu(this.ContextMenu);

            this.Loaded += LineSbcr_Loaded;
            this.MouseDown += LineSbcr_MouseDown;
            this.ContextMenu.Closed += ContextMenu_Closed;

            this.Move += LineSbcr_Move;

            this.Editing = true;

        }

        private void LineSbcr_Move(object sender, MonchaPoint3D e)
        {
           if (this.BaseContextPoint is MonchaPoint3D point1 && this.SecondContextPoint is MonchaPoint3D point2)
            {
                point2.Update(point2.X + e.X, point2.Y + e.Y);
                point1.Update(point1.X + e.X, point1.Y + e.Y);
            }
        }

        private void Basepoint_ChangePoint(object sender, MonchaPoint3D e)
        {
            GetLine();
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            MenuItem cmindex = (MenuItem)this.ContextMenu.DataContext;
            switch (cmindex.Header)
            {
                case "Fix":
                    if (this.BaseContextPoint is MonchaPoint3D point)
                    {
                        point.IsFix = !point.IsFix;
                        this._lenth = PtPLenth(new MonchaPoint3D(0, 0, 0), this.SecondContextPoint as MonchaPoint3D);
                    }
                    break;
                case "Remove":
                    if (this.Parent is CadCanvas canvas)
                    {
                        canvas.Children.Remove(this);
                    }
                    break;
            }
        }



        private void LineSbcr_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                this.ContextMenu.IsOpen = true;
            }
        }


        private void LineSbcr_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent is CadCanvas canvas)
            {
                canvas.MouseLeftButtonUp += Canvas_MouseLeftUp;
                canvas.MouseMove += Canvas_MouseMove;
                this.CaptureMouse();
                this.adornerLayer = AdornerLayer.GetAdornerLayer(canvas);
                AdornerLineDim myAdorner = new AdornerLineDim(this, canvas);
                myAdorner.DataContext = this;
                adornerLayer.Add(myAdorner);

            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (this.SecondContextPoint is MonchaPoint3D point && this.Parent is CadCanvas canvas)
                {
                    point.Update(e.GetPosition(canvas));
                }
                GetLine();
            }
            else
                this.ReleaseMouseCapture();
        }

        //Заканчиваем с линией
        private void Canvas_MouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = this.Parent as Canvas;
            canvas.MouseMove -= Canvas_MouseMove;
            canvas.MouseLeftButtonUp -= Canvas_MouseLeftUp;
            this.ReleaseMouseCapture();

            if (this.Edit != null)
                this.Edit(this, false);
            this.Editing = false;
        }


        private void GetLine()
        {
            this._lineGeometry.StartPoint = ((MonchaPoint3D)this.BaseContextPoint).GetPoint;
            this._lineGeometry.EndPoint = ((MonchaPoint3D)this.SecondContextPoint).GetPoint;

            this.UpdateLayout();

        }

        public void SubsPoint(DotShape dotShape, int indexPoint)
        {
            if (indexPoint == 0)
            {
                this.BaseContextPoint = dotShape.BaseContextPoint;
            }
            if (indexPoint == 1)
            {
                this.SecondContextPoint = dotShape.BaseContextPoint;
            }
        }

        private void DotShape_ChangePoint1(object sender, MonchaPoint3D e)
        {

            this.SecondContextPoint = new MonchaPoint3D(e.X,  e.Y, e.Z - this.BPoint.Z);
            if (!this.WasMove)
            {
                this.WasMove = true;
                if (this.BaseContextPoint is MonchaPoint3D point && point.IsFix)
                {
                    if (ChangePoint0 != null)
                    {
                        MonchaPoint3D temppoint = GetPointOnLine(this.SecondContextPoint as MonchaPoint3D, this.BaseContextPoint as MonchaPoint3D);
                        ChangePoint0(this, temppoint);
                    }
                }
                GetLine();
                this.WasMove = false;
            }
        }

        private void DotShape_ChangePoint0(object sender, MonchaPoint3D e)
        {
            this.BaseContextPoint = e;

            if (!this.WasMove)
            {
                this.WasMove = true;
                if (this.BaseContextPoint is MonchaPoint3D point && point.IsFix)
                    if (ChangePoint1 != null)
                    {
                        MonchaPoint3D temppoint = GetPointOnLine(this.BaseContextPoint as MonchaPoint3D, this.SecondContextPoint as MonchaPoint3D);
                        ChangePoint1(this, temppoint);
                    }
                        
                GetLine();
                this.WasMove = false;
            }
        }

        private MonchaPoint3D GetPointOnLine(MonchaPoint3D point0, MonchaPoint3D point1)
        {
            //находим длину исходного отрезка
            var dx = point1.X - point0.X;
            var dy = point1.Y - point0.Y;
            var l = Math.Sqrt(dx * dx + dy * dy);
            //находим направляющий вектор
            var dirX = dx / l;
            var dirY = dy / l;
            //умножаем направляющий вектор на необх длину
            dirX *= this._lenth;
            dirY *= this._lenth;
            //находим точку
            return new MonchaPoint3D(dirX + point0.X, dirY + point0.Y, 0);
        }

        public static double PtPLenth(MonchaPoint3D point1, MonchaPoint3D point2)
        {
            return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2) + Math.Pow(point2.Z - point1.Z, 2));
        }

        public double Lenth()
        {
            if (this.BaseContextPoint is MonchaPoint3D point1 && this.SecondContextPoint is MonchaPoint3D point2)
                return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2) + Math.Pow(point2.Z - point1.Z, 2));
            else
                return 0;
        }
    }
}
