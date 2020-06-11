using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MonchaSDK.Object;

namespace MonchaCadViewer.CanvasObj
{
    public class CadDot : CadObject
    {
        private Rect _rect;

        public double Size { get; set; }


        public CadDot(LPoint3D point, double Size, bool capturemouse, bool move) : base (capturemouse, move)
        {
            this.Size = Size;
            this.Focusable = true;

            Canvas.SetZIndex(this, 999);
            this._rect = new Rect(new Size(Size, Size));
            this.GmtrObj = new RectangleGeometry(_rect);

            ContextMenuLib.DotContextMenu(this.ContextMenu);

            this.ContextMenuClosing += DotShape_ContextMenuClosing;
            this.MouseLeftButtonUp += DotShape_MouseLeftButtonUp;
            this.Loaded += CadDot_Loaded;
            this.Updated += CadDot_Updated;

            // this.BaseContextPoint.ChangePoint += MonchaPoint_ChangePoint;

            UpdatePoint();


        }

        private void CadDot_Updated(object sender, CadObject e)
        {
            UpdatePoint();
        }

        private void CadDot_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent is CadCanvas canvas)
                canvas.SubsObj(this);
            this.UpdateLayout();
        }


        private void DotShape_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.Parent is CadCanvas canvas)
            {
                CadDot FindDot = canvas.UndrMouseAnchor(Mouse.GetPosition(canvas), this);
                if (FindDot != null && FindDot != this)
                {
                    this.BaseContextPoint.ReLink(FindDot.BaseContextPoint);
                    canvas.RemoveAnchor(this);
                }
            }
        }


        private void DotShape_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem cmindex)
            {
                switch (cmindex.Header)
                {
                    case "Fix":
                        if (this.BaseContextPoint is LPoint3D point)
                            point.IsFix = !point.IsFix;
                        break;
                    case "Remove":
                        this.Remove();
                        break;
                }
            }
        }

        public void UpdatePoint()
        {
            if (this.BaseContextPoint is LPoint3D point && !point.IsFix)
            {
                Canvas.SetLeft(this, this.BaseContextPoint.GetMPoint.X - Size / 2);
                Canvas.SetTop(this, this.BaseContextPoint.GetMPoint.Y - Size / 2); //Y inverted in calibration stat
                Canvas.SetZIndex(this, 999);
            }
            CadObject.StatColorSelect(this);            
        }

        public bool Contains(Point point)
        {
            return (this.BaseContextPoint.GetMPoint.X - Size / 2 < point.X && this.BaseContextPoint.GetMPoint.X + Size / 2 > point.X) 
                && (this.BaseContextPoint.GetMPoint.Y - Size / 2 < point.Y && this.BaseContextPoint.GetMPoint.Y + Size / 2 > point.Y);
        }
    }
}
