using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MonchaCadViewer.CanvasObj
{
    public class DotShape : CadObject
    {
        private RectangleGeometry _rectg;
        private Rect _rect;
        private bool _calibration;

        

        public double Size { get; set; }

        protected override Geometry DefiningGeometry => UpdatePoint();


        public DotShape(Point point, double Size, MonchaPoint3D Multiplier, bool Calibration, bool capturemouse, bool move) : base (capturemouse, new MonchaPoint3D(point.X, point.Y, 0), move)
        {
            this.Size = Size;
            this.Multiplier = Multiplier;
            this._calibration = Calibration;

            ContextMenuLib.DotContextMenu(this.ContextMenu);

            this.ContextMenuClosing += DotShape_ContextMenuClosing;
            this.MouseLeftButtonUp += DotShape_MouseLeftButtonUp;

            this.BaseContextPoint.ChangePoint += MonchaPoint_ChangePoint;
        }

        private void MonchaPoint_ChangePoint(object sender, MonchaPoint3D e)
        {
            UpdatePoint();
        }



        private void DotShape_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.Parent is CadCanvas canvas)
            {
                DotShape FindDot = canvas.UndrMouseAnchor(Mouse.GetPosition(canvas), this);
                if (FindDot != null && FindDot != this)
                {
                    this.BaseContextPoint.ReLink(FindDot.BaseContextPoint);
                    canvas.RemoveAnchor(this);
                }
            }
        }


        private void DotShape_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            MenuItem cmindex = (MenuItem)this.ContextMenu.DataContext;
            switch (cmindex.Header)
            {
                case "Fix":
                    if (this.BaseContextPoint is MonchaPoint3D point)
                        point.IsFix = !point.IsFix;
                    break;
                case "Remove":
                    if (this.Parent is CadCanvas canvas)
                    {
                        canvas.Children.Remove(this);
                    }
                    break;
            }
        }

        public Geometry UpdatePoint()
        {
            this._rect = new Rect(new Size(Size, Size));
            this._rectg = new RectangleGeometry(_rect);
            if (this.BaseContextPoint is MonchaPoint3D point && !point.IsFix)
            {
                Canvas.SetLeft(this, this.MultPoint.X - Size / 2);
                Canvas.SetTop(this, this.MultPoint.Y - Size / 2); //Y inverted in calibration stat
                Canvas.SetZIndex(this, 999);
            }

            return this._rectg;
        }

        public bool CheckInArea(Point point)
        {
            return (MultPoint.X - Size / 2 < point.X && MultPoint.X + Size / 2 > point.X) && (MultPoint.Y - Size / 2 < point.Y && MultPoint.Y + Size / 2 > point.Y);
        }
    }
}
