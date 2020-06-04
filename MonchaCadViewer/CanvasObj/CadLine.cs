using MonchaCadViewer.CanvasObj.DimObj;
using MonchaSDK.Object;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;


namespace MonchaCadViewer.CanvasObj
{
    class CadLine : CadObject
    {
        private LineGeometry _lineGeometry;
        private double _lenth;

        protected override Geometry DefiningGeometry => _lineGeometry;

        public double Lenth { get => GetLenth(); set => this._lenth = value; }
        public MonchaPoint3D SecondContextPoint { get; set; }


        public CadLine(MonchaPoint3D StartPoint, MonchaPoint3D EndPoint, bool Capturemouse) : base(Capturemouse, new MonchaPoint3D(StartPoint.X, StartPoint.Y, 0), false)
        {
            //associate and subcribe
            this.BaseContextPoint = StartPoint;
            this.BaseContextPoint.ChangePoint += ContextPoint_ChangePoint;
            this.BaseContextPoint.ChangePointDelta += ContextPoint_ChangeDeltaPoint;
            this.BaseContextPoint.Relink += ContextPoint_Relink;

            this.SecondContextPoint = EndPoint;
            this.SecondContextPoint.ChangePoint += ContextPoint_ChangePoint;
            this.SecondContextPoint.Relink += ContextPoint_Relink;

            this.Stroke = Brushes.Black;
            this.StrokeThickness = 40;
            this.Focusable = true;

            this._lineGeometry = new LineGeometry();

            ContextMenuLib.LineContextMenu(this.ContextMenu);

            this.MouseDown += LineSbcr_MouseDown;
            this.MouseEnter += LineSbcr_MouseEnter;
            this.MouseLeave += LineSbcr_MouseLeave;
            this.ContextMenu.Closed += ContextMenu_Closed;
            this.Loaded += LineSbcr_Loaded;

        }

        private void LineSbcr_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent is CadCanvas canvas)
            {
                canvas.SubsObj(this);
                this.adornerLayer = AdornerLayer.GetAdornerLayer(canvas);
                this.ObjAdorner = new AdornerLineDim(this);
                this.ObjAdorner.DataContext = this;
                adornerLayer.Add(this.ObjAdorner);
                adornerLayer.Update();
                //myAdorner.AngleChange += MyAdorner_AngleChange;
            }

        }

        private void LineSbcr_MouseLeave(object sender, MouseEventArgs e)
        {
            this.StrokeThickness = 40;
        }

        private void LineSbcr_MouseEnter(object sender, MouseEventArgs e)
        {
            this.StrokeThickness = 80;
        }

        private void ContextPoint_Relink(object sender, MonchaPoint3D e)
        {
            if (this.BaseContextPoint == sender)
            {
                this.BaseContextPoint = e;
                this.BaseContextPoint.ChangePoint += ContextPoint_ChangePoint;
            }
            else
            {
                this.SecondContextPoint = e;
                this.SecondContextPoint.ChangePoint += ContextPoint_ChangePoint;
            }
            GetLine();
        }

        private void ContextPoint_ChangePoint(object sender, MonchaPoint3D e)
        {
            if (!this.Editing)
            {
                this.Editing = true;
                if (this.BaseContextPoint == sender)
                {
                    if (this.IsFix) 
                        this.SecondContextPoint.Update(GetPointOnLine(this.BaseContextPoint, this.SecondContextPoint), false);
                }
                else
                {
                    if (this.IsFix)
                        this.BaseContextPoint.Update(GetPointOnLine(this.SecondContextPoint, this.BaseContextPoint), false);
                }
                this.Editing = false;
            }

            GetLine();
        }

        private void ContextPoint_ChangeDeltaPoint(object sender, MonchaPoint3D e)
        {
            if (this.Editing)
                this.SecondContextPoint.Add(e, false);
        }




        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                switch (menuItem.Header)
                {
                    case "Fix":
                        this.IsFix = !this.IsFix;
                        this._lenth = GetLenth();
                        break;

                    case "Remove":
                        this.Remove();
                        break;

                    case "Render":
                        this.Render = !this.Render;
                        break;
                }
            }
        }


        private void LineSbcr_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                this.ContextMenu.IsOpen = true;
            }
        }

        private void GetLine()
        {
            this._lineGeometry.StartPoint = this.BaseContextPoint.GetMPoint;
            this._lineGeometry.EndPoint = this.SecondContextPoint.GetMPoint;

            CadObject.StatColorSelect(this);
            adornerLayer.Update();

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
            return new MonchaPoint3D(dirX + point0.X, dirY + point0.Y, point1.Z);
        }

        public static double PtPLenth(MonchaPoint3D point1, MonchaPoint3D point2)
        {
            return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2) + Math.Pow(point2.Z - point1.Z, 2));
        }

        public double GetLenth()
        {
            if (this._lenth == 0)
            {

                if (this.BaseContextPoint is MonchaPoint3D point1 && this.SecondContextPoint is MonchaPoint3D point2)
                    return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2) + Math.Pow(point2.Z - point1.Z, 2));
                else
                    return 0;
            }
            else return this._lenth;
        }
    }
}
