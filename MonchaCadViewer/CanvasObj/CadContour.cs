using MonchaCadViewer.CanvasObj.DimObj;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MonchaCadViewer.CanvasObj
{
    class CadContour : CadObject
    {
        private PathGeometry _path;
        private LObjectList _points = new LObjectList();
        private bool wasmove = false;


        public bool Mirror { get; set; } = false;
        public double Angle { get; set; } = 0;

        public LObjectList Points
        {
            get => _points;
            set => _points = value;
        }

        public MonchaPoint3D BOP => getbop();
        public MonchaPoint3D TOP => gettop();
        public MonchaPoint3D CNTR => getcenter();

        public Size Size => _path.Bounds.Size;

        public Rect BoundRect => _path.Bounds;

        private MonchaPoint3D getbop()
        {
            Rect _rect = _path.Bounds;
            return new MonchaPoint3D(_rect.BottomLeft.X, _rect.BottomLeft.Y, 0);
        }

        private MonchaPoint3D gettop()
        {
            Rect _rect = _path.Bounds;
            return new MonchaPoint3D(_rect.TopRight.X, _rect.TopRight.Y, 0);
        }

        private MonchaPoint3D getcenter()
        {
            Rect _rect = _path.Bounds;
            return new MonchaPoint3D((_rect.TopRight.X - _rect.BottomLeft.X)/2, (_rect.TopRight.Y - _rect.BottomLeft.X) / 2, 0);
        }

        protected override Geometry DefiningGeometry => this._path;

        public CadContour(LObjectList point3Ds, MonchaPoint3D Center, bool maincanvas, bool Capturemouse) : base (Capturemouse, Center, false)
        {
            this._points = point3Ds;
            this.UpdateGeometry();
            this.ClipToBounds = false;

            if (maincanvas)
            {
                ContextMenuLib.ViewContourMenu(this.ContextMenu);
                this.Cursor = Cursors.Hand;
                this.MouseLeave += Contour_MouseLeave;
                this.MouseLeftButtonUp += Contour_MouseLeftUp;
                this.Loaded += ViewContour_Loaded;
                this.ContextMenuClosing += ViewContour_ContextMenuClosing;

                this.BaseContextPoint.ChangePoint += ViewContour_MoveBasePoint;
            }

            this.Fill = Brushes.Transparent;
            this.StrokeThickness = _path.Bounds.Width * 0.002;
            this.Stroke = Brushes.Red;
        }

        private void ViewContour_MoveBasePoint(object sender, MonchaPoint3D e)
        {
            this.UpdateGeometry();
        }

        private void ViewContour_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                switch (menuItem.Header)
                {
                    case "Mirror":
                        this.Mirror = !this.Mirror;
                        break;

                    case "Fix":
                        this.IsFix = !this.IsFix;
                        break;

                    case "Remove":
                        if (this.Parent is CadCanvas canvas)
                        {
                            canvas.Children.Remove(this);
                        }
                        break;

                    case "Render":
                        this.Render = !this.Render;
                        break;
                }
            }
        }

        private void ViewContour_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent is CadCanvas canvas)
            {
                this.adornerLayer = AdornerLayer.GetAdornerLayer(canvas);
                AdornerContourFrame myAdorner = new AdornerContourFrame(this, canvas);
                myAdorner.DataContext = this;
                adornerLayer.Add(myAdorner);
                myAdorner.AngleChange += MyAdorner_AngleChange;
            }
        }

        private void MyAdorner_AngleChange(object sender, double e)
        {
            this.Angle = e;
            this.UpdateGeometry(true);
        }

        private void Contour_MouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (wasmove)
            {
                this.ReleaseMouseCapture();
                this.wasmove = false;
            }
            else
            {

            }
            
        }

        private void Contour_MouseLeave(object sender, MouseEventArgs e)
        {
            this.ReleaseMouseCapture();
        }

        public List<List<MonchaPoint3D>> GiveModPoint()
        {
            List<List<MonchaPoint3D>> ListPoints = new List<List<MonchaPoint3D>>();

            for (int i = 0; i < _points.Count; i++)
            {
                List<MonchaPoint3D> Points = new List<MonchaPoint3D>();
                for (int j = 0; j < _points[i].Count; j++)
                {
                    MonchaPoint3D modPoint = _points[i][j];
                    if (this.Mirror)
                        modPoint = new MonchaPoint3D(- modPoint.X, modPoint.Y, modPoint.Z, modPoint.M);

                    if (this.Angle != 0)
                        modPoint = RotatePoint(modPoint, new MonchaPoint3D(0, 0, 0));

                    Points.Add(modPoint);
                }
                ListPoints.Add(Points);
            }

            return ListPoints;

            MonchaPoint3D RotatePoint(MonchaPoint3D pointToRotate, MonchaPoint3D centerPoint)
            {
                this.Angle = (360 + this.Angle) % 360;
                double angleInRadians = this.Angle * (Math.PI / 180);
                double cosTheta = (float)Math.Cos(angleInRadians);
                double sinTheta = (float)Math.Sin(angleInRadians);
                return new MonchaPoint3D
                (
                        //X
                        (cosTheta * (pointToRotate.X - centerPoint.X) -
                        sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                        //Y
                        (sinTheta * (pointToRotate.X - centerPoint.X) +
                        cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y),
                     //Z
                     pointToRotate.Z,
                    //M
                    pointToRotate.M
                );
            }
        }

        public void UpdateGeometry(bool FromAdorner = false)
        {
            PathFigureCollection _pathFigures = new PathFigureCollection();

            List<List<MonchaPoint3D>> workPoint = GiveModPoint();

            for (int i = 0; i < workPoint.Count; i++)
            {
                Point StartPoint = workPoint[i][0].GetPoint;
                PathSegmentCollection pathSegments = new PathSegmentCollection();
                for (int j = 0; j < _points[i].Count; j++)
                {
                    pathSegments.Add(new LineSegment(workPoint[i][j].GetPoint, true));
                }

                _pathFigures.Add(new PathFigure(StartPoint, pathSegments, true));
            }
            this._path = new PathGeometry(_pathFigures);

            if (adornerLayer != null && !FromAdorner)
                adornerLayer.Update();

            Canvas.SetLeft(this, this.BaseContextPoint.GetMPoint.X);
            Canvas.SetTop(this, this.BaseContextPoint.GetMPoint.Y);

        }
    }
}

