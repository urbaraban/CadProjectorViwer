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
    public class CadAnchor : CadObject
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
                this.PointX.MX = value;
                OnPropertyChanged("X");
            }
        }

        public override double Y
        {
            get => this.Translate.Y;
            set
            {
                this.Translate.Y = value;
                this.PointY.MY = value;
                OnPropertyChanged("Y");
            }
        }

        private LPoint3D PointX { get; set; }
        private LPoint3D PointY { get; set; }

        public LPoint3D GetPoint => PointX != PointY ? new LPoint3D(PointX.MX, PointY.MY, PointX.M) : PointX;


        public CadAnchor(LPoint3D Point, double AnchorSize, bool OnBaseMesh)
        {
            this.PointX = Point;
            this.PointX.PropertyChanged += Point_PropertyChanged;
            this.PointY = Point;

            this.PointX.Selected += Point_Selected;
            this.PropertyChanged += CadDot_PropertyChanged;

            CommonSetting(AnchorSize);
        }

        public CadAnchor(LPoint3D PointX, LPoint3D PointY, double AnchorSize, bool OnBaseMesh)
        {
            this.PointX = PointX;
            this.PointX.PropertyChanged += Point_PropertyChanged;
            this.PointY = PointY;
            this.PointY.PropertyChanged += Point_PropertyChanged;
            CommonSetting(AnchorSize);
        }

        private void CommonSetting(double AnchorSize)
        {

            this.size = AnchorSize;
            this.myGeometry = new RectangleGeometry(new Rect(-AnchorSize / 2, -AnchorSize / 2, AnchorSize, AnchorSize));
            this.ShowName = false;
            ContextMenuLib.DotContextMenu(this.ContextMenu);
            Canvas.SetZIndex(this, 999);
            this.RenderTransformOrigin = new Point(1, 1);

            this.UpdateTransform(null, false);
            this.Translate.X = this.PointX.MX;
            this.Translate.Y = this.PointY.MY;

            this.OnBaseMesh = OnBaseMesh;

            this.ContextMenuClosing += DotShape_ContextMenuClosing;
            this.MouseLeftButtonDown += CadDot_MouseLeftButtonDown;
            this.Fixed += CadDot_Fixed;
        }

        private void Point_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() => 
            { 
                this.Translate.X = this.PointX.MX;
                this.Translate.Y = this.PointY.MY;
            });
        }

        private void CadDot_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected" && this.PointX.Select == false)
            {
                PointX.Select = true;
            }
            else if (e.PropertyName == "Leave" && this.PointX.Select == true)
            {
                PointX.Select = false;
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
            this.PointX.Select = e;
        }


        private void CadDot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //this.Point.Select = true;
        }

        private void CadDot_Fixed(object sender, bool e)
        {
            this.PointX.IsFix = e;
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
                        DotEdit dotEdit = new DotEdit(this.PointX);
                        dotEdit.Show();
                        break;
                }
            }
        }
    }
}
