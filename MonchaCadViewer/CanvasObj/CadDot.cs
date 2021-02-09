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
        private double size;
        private RectangleGeometry rectangle;

        public override Pen myPen { get; } = new Pen(null, 0);
        public override Brush myBack => this.IsSelected == true ? Brushes.Red : Brushes.DarkGray;

        public override event EventHandler<string> Updated;

        public override double X 
        { 
            get => this.Translate.X;
            set
            {
                this.Translate.X = value;
                OnPropertyChanged("X");
            }
        }

        public override double Y
        {
            get => this.Translate.Y;
            set
            {
                this.Translate.Y = value;
                OnPropertyChanged("Y");
            }
        }

        public LPoint3D Point { get; set; }


        public CadDot(LPoint3D Point, double Size, bool OnBaseMesh, bool capturemouse, bool move) : base(capturemouse, move )
        {
            this.myGeometry = new RectangleGeometry(new Rect(-Size / 2, -Size / 2, Size, Size));

            this.RenderTransformOrigin = new Point(1, 1);

            this.UpdateTransform(null);

            this.OnBaseMesh = OnBaseMesh;

            this.Point = Point;
            this.Point.PropertyChanged += Point_PropertyChanged;

            this.Translate.X = Point.MX;
            this.Translate.Y = Point.MY;

            this.size = Size;
            this.ShowName = false;

            Canvas.SetZIndex(this, 999);

            ContextMenuLib.DotContextMenu(this.ContextMenu);

            this.ContextMenuClosing += DotShape_ContextMenuClosing;
            this.MouseLeftButtonDown += CadDot_MouseLeftButtonDown;
            this.Fixed += CadDot_Fixed;
            this.Point.Selected += Point_Selected;
            this.PropertyChanged += CadDot_PropertyChanged;
        }

        private void Point_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() => 
            { 
                this.Translate.X = this.Point.MX;
                this.Translate.Y = this.Point.MY;
            });
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

            this.InvalidateVisual();
            Updated?.Invoke(this, null);
        }

        private void Point_Selected(object sender, bool e)
        {
            this.Render = e;
        }

        private void CadDot_Selected(object sender, bool e)
        {
            this.Point.Select = e;
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
