using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CadProjectorViewer.Calibration;
using CadProjectorSDK;
using CadProjectorSDK.Device;
using CadProjectorSDK.CadObjects;

namespace CadProjectorViewer.CanvasObj
{
    public class CadAnchor : CadObject
    {
        private double size;
        private RectangleGeometry rectangle;

        public override Pen myPen { get; } = new Pen(null, 0);
        public override Brush myBack => this.IsSelected == true ? Brushes.Red : Brushes.DarkGray;

        public override event EventHandler<string> Updated;

        public override Rect Bounds => new Rect(-this.size / 2, -this.size / 2, this.size, this.size);

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

        private CadPoint3D PointX { get; set; }
        private CadPoint3D PointY { get; set; }

        public CadPoint3D GetLPoint => PointX != PointY ? new CadPoint3D(PointX.MX, PointY.MY, PointX.M) : PointX;


        public CadAnchor(CadPoint3D Point) : base(true)
        {
            this.PointX = Point;
            this.PointX.PropertyChanged += Point_PropertyChanged;
            this.PointY = Point;

            this.PointX.Selected += Point_Selected;
            this.PropertyChanged += CadDot_PropertyChanged;

            CommonSetting();
        }

        public CadAnchor(CadPoint3D PointX, CadPoint3D PointY) : base(true)
        {
            this.PointX = PointX;
            this.PointX.PropertyChanged += Point_PropertyChanged;
            this.PointY = PointY;
            this.PointY.PropertyChanged += Point_PropertyChanged;
            CommonSetting();
        }

        private void CommonSetting()
        {
            this.Cursor = Cursors.SizeAll;
            this.AllowDrop = true;
            this.Drop += CadAnchor_Drop;

            this.size = ProjectorHub.GetThinkess * 2;
            this.ShowName = false;
            ContextMenuLib.DotContextMenu(this.ContextMenu);
            Canvas.SetZIndex(this, 999);
            this.RenderTransformOrigin = new Point(1, 1);

            this.UpdateTransform(false);
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
            if (e.PropertyName == "IsSelected" && this.PointX.IsSelected == false)
            {
                PointX.IsSelected = true;
            }
            else if (e.PropertyName == "Leave" && this.PointX.IsSelected == true)
            {
                PointX.IsSelected = false;
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
            this.PointX.IsSelected = e;
        }


        private void CadAnchor_Drop(object sender, DragEventArgs e)
        {
            if (e.Data is Tuple<CadPoint3D, CadPoint3D> points)
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
