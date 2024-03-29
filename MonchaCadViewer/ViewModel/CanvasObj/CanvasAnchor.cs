﻿using CadProjectorSDK.CadObjects;
using CadProjectorViewer.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.CanvasObj
{
    internal class CanvasAnchor : CanvasObject
    {
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
            this.Cursor = System.Windows.Input.Cursors.SizeAll;
            CommonSetting();
        }

        private void CommonSetting()
        {
            this.ShowName = false;
            //ContextMenuLib.DotContextMenu(this.ContextMenu);
            Canvas.SetZIndex(this, 999);
            this.RenderTransformOrigin = new Point(1, 1);

            this.CadObject.Translate.OffsetX = this.Point.MX;
            this.CadObject.Translate.OffsetY = this.Point.MY;

            this.ContextMenuClosing += DotShape_ContextMenuClosing;

            ContextMenu.Items.Add(new Separator());
            ContextMenuLib.AddItem("common_Edit", RenderCommand, this.ContextMenu);
        }


        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
             base.OnMouseDown(e);
        }

        private void DotShape_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is System.Windows.Controls.MenuItem cmindex)
            {
                switch (cmindex.Tag)
                {
                    case "common_Remove":
                        this.CadObject.Remove();
                        break;
                }
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            RenderDeviceModel deviceModel = this.GetViewModel?.Invoke();
            double _size = 1;
            var ProportionPoint = new Point(0, 0);
            var RenderPoint = new Point(0, 0);
            if (deviceModel != null)
            {
                _size = deviceModel.Thinkess * 4;
                ProportionPoint = deviceModel.GetProportion(this.Point.MX, this.Point.MY);
                RenderPoint = deviceModel.GetPoint(ProportionPoint.X, ProportionPoint.Y);
            }

            drawingContext.PushTransform(new TranslateTransform(RenderPoint.X, RenderPoint.Y));
            drawingContext.DrawGeometry(
                myBack, 
                new Pen(Brushes.Black, _size * 0.1), 
                new RectangleGeometry(new Rect(-_size / 2, -_size / 2, _size, _size)));

        }
    }
}
