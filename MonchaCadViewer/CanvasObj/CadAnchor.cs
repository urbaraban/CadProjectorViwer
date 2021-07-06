using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MonchaCadViewer.Calibration;
using MonchaSDK;
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
            get => this.Translate.OffsetX;
            set
            {
                this.Translate.OffsetX = value;
                this.PointX.MX = value;
                OnPropertyChanged("X");
            }
        }

        public override double Y
        {
            get => this.Translate.OffsetY;
            set
            {
                this.Translate.OffsetY = value;
                this.PointY.MY = value;
                OnPropertyChanged("Y");
            }
        }

        private LPoint3D PointX { get; set; }
        private LPoint3D PointY { get; set; }

        public LPoint3D GetLPoint => PointX != PointY ? new LPoint3D(PointX.MX, PointY.MY, PointX.M) : PointX;


        public CadAnchor(LPoint3D Point, bool OnBaseMesh)
        {
            this.PointX = Point;
            this.PointX.PropertyChanged += Point_PropertyChanged;
            this.PointY = Point;

            this.PointX.Selected += Point_Selected;
            this.PropertyChanged += CadDot_PropertyChanged;

            CommonSetting();
        }

        public CadAnchor(LPoint3D PointX, LPoint3D PointY, bool OnBaseMesh)
        {
            this.PointX = PointX;
            this.PointX.PropertyChanged += Point_PropertyChanged;
            this.PointY = PointY;
            this.PointY.PropertyChanged += Point_PropertyChanged;
            CommonSetting();
        }

        private void CommonSetting()
        {
            this.AllowDrop = true;
            this.Drop += CadAnchor_Drop;

            this.size = MonchaHub.GetThinkess * 2;
            this.ShowName = false;
            ContextMenuLib.DotContextMenu(this.ContextMenu);
            Canvas.SetZIndex(this, 999);
            this.RenderTransformOrigin = new Point(1, 1);

            this.UpdateTransform(null, false, new Rect(-this.size / 2, -this.size / 2, this.size, this.size));
            this.Translate.OffsetX = this.PointX.MX;
            this.Translate.OffsetY = this.PointY.MY;

            this.ContextMenuClosing += DotShape_ContextMenuClosing;
            this.Fixed += CadDot_Fixed;
        }

        private void Point_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() => 
            { 
                this.Translate.OffsetX = this.PointX.MX;
                this.Translate.OffsetY = this.PointY.MY;
                this.InvalidateVisual();
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


        private void CadAnchor_Drop(object sender, DragEventArgs e)
        {
            if (e.Data is Tuple<LPoint3D, LPoint3D> points)
            {
                this.PointX = points.Item1;
                this.PointY = points.Item2;
            }
        }

        private void CadDot_Fixed(object sender, bool e)
        {
            this.PointX.IsFix = e;
        }

        private void DotShape_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem cmindex)
            {
                switch (cmindex.Tag)
                {
                    case "obj_Fix":
                        this.IsFix = !this.IsFix;
                        break;
                    case "common_Remove":
                        this.Remove();
                        break;
                    case "common_Edit":
                        DotEdit dotEdit = new DotEdit(this.PointX);
                        dotEdit.Show();
                        break;
                }
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.PushTransform(new TranslateTransform(X, Y));
            drawingContext.DrawGeometry(myBack, myPen, new RectangleGeometry(new Rect(-this.size / 2, -this.size / 2, this.size, this.size)));
        }

    }
}
