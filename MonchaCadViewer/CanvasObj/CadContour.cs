using MonchaCadViewer.CanvasObj.DimObj;
using MonchaSDK;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MonchaCadViewer.CanvasObj
{
    class CadContour : CadObject
    {
        private LObjectList _points;
        private bool _maincanvas;
        private AdornerContourFrame adornerContour;


        public CadContour(PathGeometry Path, bool maincanvas, bool Capturemouse) : base (Capturemouse, false)
        {
            this.GmtrObj = Path;
            this._maincanvas = maincanvas;

            if (this._maincanvas)
            {
                ContextMenuLib.ViewContourMenu(this.ContextMenu);
                this.Cursor = Cursors.Hand;
                this.MouseLeave += Contour_MouseLeave;
                this.MouseLeftButtonUp += Contour_MouseLeftUp;
                this.Loaded += ViewContour_Loaded;
                this.ContextMenuClosing += ViewContour_ContextMenuClosing;
                this.MouseWheel += CadContour_MouseWheel;
            }

            this.Fill = Brushes.Transparent;
            this.StrokeThickness = MonchaHub.GetThinkess() * 0.5;
            this.Stroke = Brushes.Red;

            TranslateTransform translateTransform = new TranslateTransform(MonchaHub.Size.GetMPoint.X / 2, MonchaHub.Size.GetMPoint.Y / 2);

            this.GmtrObj.Transform = this.Transform;
        }

        private void CadContour_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.Rotate.Angle += Math.Abs(e.Delta)/e.Delta * (Keyboard.Modifiers == ModifierKeys.Shift ? 1 : 5);
        }

        private void CadContour_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
        }

        private void ViewContour_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                switch (menuItem.Header)
                {
                    case "Mirror":
                        this.Mirror = !this.Mirror;
                        this.Scale.ScaleX *= -1;
                        break;

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

        private void ViewContour_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent is CadCanvas canvas && this._maincanvas)
            {
                canvas.SubsObj(this);
                this.adornerLayer = AdornerLayer.GetAdornerLayer(canvas);

                this.ObjAdorner = new AdornerContourFrame(this);
                this.ObjAdorner.Visibility = Visibility.Hidden;
                this.ObjAdorner.DataContext = this;

                adornerLayer.Add(this.ObjAdorner);
                this.adornerContour = this.ObjAdorner as AdornerContourFrame;
                this.adornerContour.Rotation = this.Rotate;
            }
        }


        private void Contour_MouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (this.WasMove)
            {
                this.ReleaseMouseCapture();
                this.WasMove = false;
            }
            else
            {

            }
            
        }

        private void Contour_MouseLeave(object sender, MouseEventArgs e)
        {
            this.ReleaseMouseCapture();
        }

        public List<List<LPoint3D>> GiveModPoint()
        {
            List<List<LPoint3D>> ListPoints = new List<List<LPoint3D>>();

            for (int i = 0; i < _points.Count; i++)
            {
                List<LPoint3D> Points = new List<LPoint3D>();
                for (int j = 0; j < _points[i].Count; j++)
                {
                    LPoint3D modPoint = new LPoint3D(_points[i][j]);
                    if (this.Mirror)
                        modPoint.Update(-modPoint.X, modPoint.Y, modPoint.Z);

                    if (this.Angle != 0)
                        modPoint = RotatePoint(modPoint, new LPoint3D(0, 0, 0));

                    Points.Add(modPoint);
                }
                ListPoints.Add(Points);
            }

            return ListPoints;

            LPoint3D RotatePoint(LPoint3D pointToRotate, LPoint3D centerPoint)
            {
                this.Angle = (360 + this.Angle) % 360;
                double angleInRadians = this.Angle * (Math.PI / 180);
                double cosTheta = (float)Math.Cos(angleInRadians);
                double sinTheta = (float)Math.Sin(angleInRadians);
                return new LPoint3D
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
    }
}

