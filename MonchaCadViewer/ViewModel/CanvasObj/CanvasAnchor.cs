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
using AppSt = CadProjectorViewer.Properties.Settings;
using CadProjectorViewer.ViewModel;
using CadProjectorSDK.CadObjects.Interfaces;

namespace CadProjectorViewer.CanvasObj
{
    public class CanvasAnchor : CanvasObject
    {
        private RectangleGeometry rectangle;

        public override Pen GetPen(double StrThink, bool MouseOver, bool Selected, bool Render, bool Blank, SolidColorBrush DefBrush)
        {
            return new Pen(null, 1);
        }
        public override Brush myBack
        {
            get
            {
                if (this.CadObject.IsSelected == true) return Brushes.Red;
                else if (this.CadObject.IsFix == true) return Brushes.Black;
                return Brushes.DarkGray;
            }
        }

        public override Rect Bounds 
        {
            get
            {
                double _size = 4;
                if (this.GetViewModel?.Invoke() is RenderDeviceModel deviceModel)
                {
                    _size = deviceModel.Thinkess * 2 * AppSt.Default.anchor_size;
                }
                return  new Rect(-_size / 2, -_size / 2, _size, _size);
            }

        }

        public override double X
        {
            get => Point.MX;
            set
            {
                Point.MX = value;
                OnPropertyChanged("X");
            }
        }
        public override double Y
        {
            get => Point.MY;
            set
            {
                Point.MY = value;
                OnPropertyChanged("Y");
            }
        }

        public new object ToolTip => $"X:{Point.MX} Y:{Point.MY}";

        private CadAnchor Point  => (CadAnchor)this.CadObject;

        public CanvasAnchor(CadAnchor Point) : base(Point, true)
        {
            this.Cursor = Cursors.SizeAll;
            CommonSetting();
        }

        private void CommonSetting()
        {
            this.ShowName = false;
            ContextMenuLib.DotContextMenu(this.ContextMenu);
            Canvas.SetZIndex(this, 999);
            this.RenderTransformOrigin = new Point(1, 1);

            this.CadObject.Translate.OffsetX = this.Point.MX;
            this.CadObject.Translate.OffsetY = this.Point.MY;

            this.ContextMenuClosing += DotShape_ContextMenuClosing;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
             base.OnMouseDown(e);
        }

        private void DotShape_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem cmindex)
            {
                switch (cmindex.Tag)
                {
                    case "common_Remove":
                        this.CadObject.Remove();
                        break;
                    case "common_Edit":
                        DotEdit dotEdit = new DotEdit() { DataContext = this };
                        dotEdit.Show();
                        break;
                }
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            RenderDeviceModel deviceModel = this.GetViewModel?.Invoke();
            double _size = deviceModel.Thinkess * 4;

            Point ProportionPoint = deviceModel.GetProportion(this.Point.MX, this.Point.MY);
            Point RenderPoint = deviceModel.GetPoint(ProportionPoint.X, ProportionPoint.Y);

            drawingContext.PushTransform(new TranslateTransform(RenderPoint.X, RenderPoint.Y));
            drawingContext.DrawGeometry(
                myBack, 
                new Pen(Brushes.Black, _size * 0.1), 
                new RectangleGeometry(new Rect(-_size / 2, -_size / 2, _size, _size)));
        }
    }
}
