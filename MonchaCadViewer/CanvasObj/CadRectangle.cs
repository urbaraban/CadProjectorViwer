using MonchaSDK.Object;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MonchaCadViewer.CanvasObj
{
    public class CadRectangle : CadObject
    {
        

        private RectangleGeometry _rectg;
        private Rect _rect;

        public double Size { get; set; }

        public MonchaPoint3D SecondContextPoint { get; set; }


        protected override Geometry DefiningGeometry => this._rectg;

        public CadRectangle(bool mouseevent, MonchaPoint3D basePoint, MonchaPoint3D secondPoint, bool move) : base (mouseevent, basePoint, move)
        {
            this.SecondContextPoint = secondPoint;
            this.Stroke = Brushes.Gray;
            this.StrokeThickness = 20;
            this.Fill = Brushes.Transparent;
            this.SecondContextPoint = secondPoint;
            this.BaseContextPoint.ChangePoint += BaseContextPoint_ChangePoint;
            this.BaseContextPoint.ChangePointDelta += BaseContextPoint_ChangePointDelta;
            this.SecondContextPoint.ChangePoint += BaseContextPoint_ChangePoint;
            this.Loaded += CadRectangle_Loaded;
            this._rect = new Rect();
            this._rectg = new RectangleGeometry(this._rect);
            this.Update();
        }

        private void BaseContextPoint_ChangePointDelta(object sender, MonchaPoint3D e)
        {
            Console.WriteLine("Delta " + e);
            if (this.WasMove)
                this.SecondContextPoint.Add(e, true);
        }

        private void CadRectangle_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent is CadCanvas canvas)
                canvas.SubsObj(this);
        }

        private void BaseContextPoint_ChangePoint(object sender, MonchaPoint3D e)
        {
            this.Update();
        }

        public async void Update()
        {
            this.SecondContextPoint.X = this.BaseContextPoint.X > this.SecondContextPoint.X ? this.BaseContextPoint.X : this.SecondContextPoint.X;
            this.SecondContextPoint.Y = this.BaseContextPoint.Y > this.SecondContextPoint.Y ? this.BaseContextPoint.Y : this.SecondContextPoint.Y;

            this._rect.X = this.BaseContextPoint.GetMPoint.X;
            this._rect.Y = this.BaseContextPoint.GetMPoint.Y;
            this._rect.Width =  this.SecondContextPoint.GetMPoint.X - this.BaseContextPoint.GetMPoint.X;
            this._rect.Height = this.SecondContextPoint.GetMPoint.Y - this.BaseContextPoint.GetMPoint.Y;
            //this._rect = new Rect(this.BaseContextPoint.GetMPoint, this.SecondContextPoint.GetMPoint);
            this._rectg.Rect = this._rect;

            this.intEvent();
        }


    }
}
