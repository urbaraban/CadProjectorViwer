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
    public class CanvasAnchor : CanvasObject
    {
        private double size;
        private RectangleGeometry rectangle;

        public override Pen myPen { get; } = new Pen(null, 0);
        public override Brush myBack
        {
            get
            {
                if (this.CadObject.IsSelected == true) return Brushes.Red;
                else if (this.CadObject.IsFix == true) return Brushes.Black;
                return Brushes.DarkGray;
            }
        }

        public override Rect Bounds => new Rect(-this.size / 2, -this.size / 2, this.size, this.size);

        private CadAnchor Point  => (CadAnchor)this.CadObject;

        //public CadPoint3D GetLPoint => PointX != PointY ? new CadPoint3D(PointX.MX, PointY.MY, PointX.M) : PointX;


        public CanvasAnchor(CadAnchor Point) : base(Point, true)
        {
            this.Cursor = Cursors.SizeAll;
            CommonSetting();
        }

        private void CommonSetting()
        {
            this.size = GetThinkess * 2;
            this.ShowName = false;
            ContextMenuLib.DotContextMenu(this.ContextMenu);
            Canvas.SetZIndex(this, 999);
            this.RenderTransformOrigin = new Point(1, 1);

            this.CadObject.Translate.OffsetX = this.Point.X;
            this.CadObject.Translate.OffsetY = this.Point.Y;

            this.ContextMenuClosing += DotShape_ContextMenuClosing;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) 
            {
                DotEdit dotEdit = new DotEdit() { DataContext = this.Point };
                dotEdit.Show();
            }
            else base.OnMouseDown(e);

        }

        private void DotShape_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem cmindex)
            {
                switch (cmindex.Tag)
                {
                    case "obj_Fix":
                        this.CadObject.IsFix = !this.CadObject.IsFix;
                        break;
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
            drawingContext.PushTransform(new TranslateTransform(X, Y));
            drawingContext.DrawGeometry(myBack, myPen, new RectangleGeometry(new Rect(-this.size / 2, -this.size / 2, this.size, this.size)));
        }
    }
}
