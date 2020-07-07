using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MonchaCadViewer.Calibration;
using MonchaSDK.Device;
using MonchaSDK.Object;

namespace MonchaCadViewer.CanvasObj
{
    public class CadDot : CadObject
    {
        private bool Translated = false;
        private Rect rect;
        private double size;

        public LPoint3D Point { get; set; }


        public CadDot(LPoint3D Point, double Size, bool capturemouse, bool move) : base (capturemouse, move, new RectangleGeometry(new Rect(new Size(Size, Size))))
        {
            this.Point = Point;
            this.size = Size;

            if (this.GmtrObj is RectangleGeometry rectangle)
            {
                rect = rectangle.Rect;
            }

            this.Focusable = true;

            Canvas.SetZIndex(this, 999);

            ContextMenuLib.DotContextMenu(this.ContextMenu);

            this.ContextMenuClosing += DotShape_ContextMenuClosing;
            this.MouseLeftButtonUp += CadDot_MouseLeftButtonUp;
            this.Fixed += CadDot_Fixed;

            this.Translate.X = Point.GetMPoint.X - this.size / 2;
            this.Translate.Y = Point.GetMPoint.Y - this.size / 2;


            this.Translate.Changed += Translate_Changed;
            this.Point.ChangePoint += Point_ChangePoint;

            this.Fill = Brushes.Black;

        }

        private void CadDot_Fixed(object sender, bool e)
        {
            this.Point.IsFix = e;
        }

        private void CadDot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!this.WasMove)
            {
                this.Render = this.IsSelected;
            }

        }

        private void Point_ChangePoint(object sender, LPoint3D e)
        {
            if (!Translated)
            {
                Translated = true;
                this.Translate.X = this.Point.GetMPoint.X - this.size / 2;
                this.Translate.Y = this.Point.GetMPoint.Y - this.size / 2;
                Translated = false;
            }

        }

        private void Translate_Changed(object sender, System.EventArgs e)
        {
            if (!Translated)
            {
                Translated = true;
                this.Point.Set(this.Translate.X + this.size / 2, this.Translate.Y + this.size / 2);

                if (this.DataContext is MonchaDeviceMesh mesh)
                {
                    if (mesh.OnlyEdge)
                        mesh.OnEdge();
                    else
                        mesh.MorphMesh(this.Point);
                }

                Translated = false;

            }
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
            if (this.GmtrObj is RectangleGeometry rectangle)
            {
                return (this.Point.GetMPoint.X - rectangle.Rect.Size.Width / 2 < point.X && this.Point.GetMPoint.X + rectangle.Rect.Size.Width / 2 > point.X) 
                && (this.Point.GetMPoint.Y - rectangle.Rect.Size.Height / 2 < point.Y && this.Point.GetMPoint.Y + rectangle.Rect.Size.Height / 2 > point.Y);
            }
            return false;
        }
    }
}
