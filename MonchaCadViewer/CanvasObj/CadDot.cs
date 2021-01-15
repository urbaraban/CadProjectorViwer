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


        public CadDot(LPoint3D Point, double Size, bool OnBaseMesh, bool capturemouse, bool move) : base(capturemouse, move )
        {
            this.rectangle = new RectangleGeometry(new Rect(new Size(Size, Size)));

            this.ObjectShape = new Path
            {
                Data = this.rectangle
            };

            this.UpdateTransform(null);
            this.OnBaseMesh = OnBaseMesh;

            this.Point = Point;

            this.Translate.X = Point.MX;
            this.Translate.Y = Point.MY;

            this.Point.PropertyChanged += Point_PropertyChanged;
            this.Removed += CadDot_Removed;

            this.size = Size;

            this.Focusable = true;

            Canvas.SetZIndex(this, 999);

            ContextMenuLib.DotContextMenu(this.ContextMenu);

            this.ContextMenuClosing += DotShape_ContextMenuClosing;
            this.MouseLeftButtonDown += CadDot_MouseLeftButtonDown;
            this.Fixed += CadDot_Fixed;
            this.Point.Selected += Point_Selected;
            this.PropertyChanged += CadDot_PropertyChanged;
            this.Fill = Brushes.Black;

        }

        private void CadDot_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected" && this.Point.Select == false)
            {
                this.Point.Select = true;
            }
            else if (e.PropertyName == "Leave" && this.Point.Select == true)
            {
                this.Point.Select = false;
            }
            else if (e.PropertyName == "X")
            {
                this.Point.MX = this.X;
            }
            else if (e.PropertyName == "Y")
            {
                this.Point.MY = this.Y;
            }
        }

        private void Point_Selected(object sender, bool e)
        {
            this.Render = e;
        }

        private void CadDot_Selected(object sender, bool e)
        {
            this.Point.Select = e;
        }

        private void Point_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
             if (e.PropertyName == "MX" || e.PropertyName == "MY" || e.PropertyName == "Point")
            {
                Dispatcher.Invoke(() => { 
                this.Translate.X = this.Point.MX;
                this.Translate.Y = this.Point.MY;
                this.Update();
                });
            }
        }

        private void CadDot_Removed(object sender, CadObject e)
        {
          
        }

        private void CadDot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //this.Point.Select = true;
        }

        private void CadDot_Fixed(object sender, bool e)
        {
            this.Point.IsFix = e;
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
    }
}
