using MonchaCadViewer.CanvasObj.DimObj;
using MonchaSDK.Object;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace MonchaCadViewer.CanvasObj
{
    class LineSbcr : CadObject
    {
        private LineGeometry _lineGeometry;
        private double _lenth;

        protected override Geometry DefiningGeometry => _lineGeometry;

        


        public MonchaPoint3D SecondContextPoint { get; set; }


        public LineSbcr(MonchaPoint3D StartPoint, MonchaPoint3D EndPoint, bool Capturemouse) : base(Capturemouse, new MonchaPoint3D(StartPoint.X, StartPoint.Y, 0), false)
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

            this._lineGeometry = new LineGeometry();

            ContextMenuLib.LineContextMenu(this.ContextMenu);

            this.MouseDown += LineSbcr_MouseDown;
            this.ContextMenu.Closed += ContextMenu_Closed;

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
            MenuItem cmindex = (MenuItem)this.ContextMenu.DataContext;
            switch (cmindex.Header)
            {
                case "Fix":
                        this.IsFix = !this.IsFix;
                        this._lenth = PtPLenth(this.BaseContextPoint, this.SecondContextPoint);

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

        private void GetLine()
        {
            this._lineGeometry.StartPoint = ((MonchaPoint3D)this.BaseContextPoint).GetPoint;
            this._lineGeometry.EndPoint = ((MonchaPoint3D)this.SecondContextPoint).GetPoint;

            this.UpdateLayout();

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

        public double Lenth()
        {
            if (this.BaseContextPoint is MonchaPoint3D point1 && this.SecondContextPoint is MonchaPoint3D point2)
                return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2) + Math.Pow(point2.Z - point1.Z, 2));
            else
                return 0;
        }
    }
}
