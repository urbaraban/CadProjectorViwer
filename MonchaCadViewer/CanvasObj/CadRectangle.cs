using MonchaSDK.Object;
using System.Windows;
using System.Windows.Media;

namespace MonchaCadViewer.CanvasObj
{
    public class CadRectangle : CadObject
    {
        private RectangleGeometry _rectg;
        private Rect _rect;
        private bool _calibration;

        public double Size { get; set; }

        public MonchaPoint3D SecondContextPoint { get; set; }


        protected override Geometry DefiningGeometry => this._rectg;

        public CadRectangle(bool mouseevent, MonchaPoint3D basePoint, MonchaPoint3D secondPoint, bool move) : base (mouseevent, basePoint, move)
        {
            this.SecondContextPoint = secondPoint;
            this.Stroke = Brushes.Gray;
        }

        public Geometry Update()
        {
            this._rect = new Rect(this.BaseContextPoint.GetMPoint, this.SecondContextPoint.GetMPoint);
            this._rectg = new RectangleGeometry(_rect);

            return this._rectg;
        }
    }
}
