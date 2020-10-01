using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MonchaCadViewer.Calibration;
using MonchaSDK.Device;
using MonchaSDK.Object;

namespace MonchaCadViewer.CanvasObj
{
    public class CadDot : CadObject
    {
        private bool Translated = false;
        private double size;
        private RectangleGeometry rectangle;

        public LPoint3D Point { get; set; }


        public CadDot(LPoint3D Point, double Size, bool capturemouse, bool move) : base(capturemouse, move )
        {
            this.rectangle = new RectangleGeometry(new Rect(new Size(Size, Size)));

            this.ObjectShape = new Path
            {
                Data = this.rectangle
            };


            this.Point = Point;
            this.Point.Selected += Point_Selected;
            this.Point.ChangePoint += Point_ChangePoint;

            this.X = Point.MX - this.size / 2;
            this.Y = Point.MY - this.size / 2;

            this.TranslateChanged += CadDot_TranslateChanged;

            this.size = Size;

            this.Focusable = true;

            Canvas.SetZIndex(this, 999);

            ContextMenuLib.DotContextMenu(this.ContextMenu);

            this.ContextMenuClosing += DotShape_ContextMenuClosing;
            this.MouseLeftButtonUp += CadDot_MouseLeftButtonUp;
            this.MouseLeftButtonDown += CadDot_MouseLeftButtonDown;
            this.Fixed += CadDot_Fixed;

            this.Fill = Brushes.Black;

        }

        private void CadDot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //this.Point.Select = true;
        }

        private void Point_Selected(object sender, bool e)
        {
            this.IsSelected = e;
        }

        private void CadDot_Fixed(object sender, bool e)
        {
            this.Point.IsFix = e;
        }

        private void CadDot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.WasMove == false)
            {
                this.Render = this.IsSelected;
            }

        }

        private void CadDot_TranslateChanged(object sender, Rect e)
        {
            if (Translated == false)
            {
                this.Point.Set(this.X, this.Y);
            }

        }

        private void Point_ChangePoint(object sender, LPoint3D e)
        {
            Translated = true;
            this.X = this.Point.GetMPoint.X;
            this.Y = this.Point.GetMPoint.Y;
            Translated = false;

        }


        private void DotShape_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem cmindex)
            {
                switch (cmindex.Header)
                {
                    case "Fix":
                        this.IsFix = !this.IsFix;
                        break;
                    case "Remove":
                        this.Remove();
                        break;
                    case "Edit":
                        DotEdit dotEdit = new DotEdit(this.Point);
                        dotEdit.Show();
                        break;
                }
            }
        }

        public bool Contains(Point point)
        {
            return (this.Point.GetMPoint.X - this.rectangle.Rect.Size.Width / 2 < point.X && this.Point.GetMPoint.X + this.rectangle.Rect.Size.Width / 2 > point.X)
            && (this.Point.GetMPoint.Y - this.rectangle.Rect.Size.Height / 2 < point.Y && this.Point.GetMPoint.Y + this.rectangle.Rect.Size.Height / 2 > point.Y);
        }
    }
}
