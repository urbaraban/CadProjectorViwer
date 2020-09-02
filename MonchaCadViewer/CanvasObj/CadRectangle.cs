using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MonchaSDK.Object;

namespace MonchaCadViewer.CanvasObj
{
    public class CadRectangle : CadObject
    {
        private LPoint3D BasePoint = new LPoint3D();
        private LPoint3D SecondPoint = new LPoint3D();

        private RectangleGeometry _rectg;
        private Rect rect = new Rect();

        public double Size { get; set; }

        

        protected override Geometry DefiningGeometry => this._rectg;

        public CadRectangle(bool mouseevent, LPoint3D basePoint, LPoint3D secondPoint, bool move) : base (mouseevent, move, new RectangleGeometry(
            new Rect(basePoint.GetMPoint.X, basePoint.GetMPoint.Y, secondPoint.GetMPoint.X - basePoint.GetMPoint.X, secondPoint.GetMPoint.Y - basePoint.GetMPoint.Y)))
        {
            this.BasePoint = basePoint;
            this.SecondPoint = secondPoint;

            this._rectg = new RectangleGeometry();
            this.rect = new Rect();

            this.rect.Width = this.SecondPoint.GetMPoint.X - this.BasePoint.GetMPoint.X;
            this.rect.Height = this.SecondPoint.GetMPoint.Y - this.BasePoint.GetMPoint.Y;

            this.rect.X = this.BasePoint.GetMPoint.X;
            this.rect.Y = this.BasePoint.GetMPoint.Y;

            this._rectg.Rect = this.rect;

            this.Stroke = Brushes.Gray;
            this.StrokeThickness = 20;
            this.Fill = Brushes.Transparent;

            basePoint.ChangePoint += BaseContextPoint_ChangePoint;
            secondPoint.ChangePoint += BaseContextPoint_ChangePoint;
            this.TranslateChanged += Translate_Changed;

            this.ContextMenuClosing += ContextMenu_Closed;
        }

        private void Translate_Changed(object sender, Rect e)
        {
            this.BasePoint.Set(e.TopLeft);
            this.SecondPoint.Set(e.BottomRight);
        }

        private void BaseContextPoint_ChangePoint(object sender, LPoint3D e)
        {
           /* this.SecondPoint.X = this.BasePoint.X > this.SecondPoint.X ? this.BasePoint.X : this.SecondPoint.X;
            this.SecondPoint.Y = this.BasePoint.Y > this.SecondPoint.Y ? this.BasePoint.Y : this.SecondPoint.Y;

            this.rect.X = this.BasePoint.GetMPoint.X;
            this.rect.Y = this.BasePoint.GetMPoint.Y;

            this.Scale.ScaleX = this.rect.Width / (this.SecondPoint.GetMPoint.X - this.BasePoint.GetMPoint.X);
            this.Scale.ScaleY = this.rect.Height / (this.SecondPoint.GetMPoint.Y - this.BasePoint.GetMPoint.Y);*/
        }


        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                switch (menuItem.Header)
                {
                    case "Fix":
                        this.IsFix = !this.IsFix;
                        break;

                    case "Remove":
                        this.Remove();
                        break;

                    case "Render":
                        this.Render = !this.Render;
                        break;
                }
            }
        }


    }
}
