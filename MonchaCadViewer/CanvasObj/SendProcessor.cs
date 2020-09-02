using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System;

namespace MonchaCadViewer.CanvasObj
{
    public static class SendProcessor
    {
        /// <summary>
        /// Обрабатывает объекты и готовит их на лазер
        /// </summary>
        /// <param name="canvas">Отправляемое рабочее поле</param>
        /// <returns>N_{i,degree}(step)</returns>
        public static void Worker(CadCanvas canvas)
        {
            LObjectList tempList = new LObjectList();
            foreach (CadObject cadObject in canvas.Children)
            {
                if (cadObject.Render)
                {
                    tempList.AddRange(GetPoint(cadObject));
                }

                tempList.OnBaseMesh = cadObject.OnBaseMesh;
            }

            if (tempList.Count > 0)
            {
                MonchaHub.MainFrame = tempList;
                MonchaHub.RefreshFrame();
            }
        }

        /// <summary>
        /// Send work rectangle projector
        /// </summary>
        /// <param name="device">The right device</param>
        /// <returns>N_{i,degree}(step)</returns>
        public static void DrawZone(MonchaDevice device)
        {
            LObjectList tempList = new LObjectList();

            tempList.Bop = new LPoint3D(0, 0, 0);
            tempList.Top = new LPoint3D(1, 1, 1);

            LObject lObject = new LObject();
            lObject.Add(new LPoint3D(device.TBOP.X * MonchaHub.Size.X, device.TBOP.Y * MonchaHub.Size.Y, 0, 1));
            lObject.Add(new LPoint3D(device.TTOP.X * MonchaHub.Size.X * 0.99, device.TBOP.Y * MonchaHub.Size.Y, 0, 1));
            lObject.Add(new LPoint3D(device.TTOP.X * MonchaHub.Size.X * 0.99, device.TTOP.Y * MonchaHub.Size.Y * 0.99, 0, 1));
            lObject.Add(new LPoint3D(device.TBOP.X * MonchaHub.Size.X * 0.99, device.TTOP.Y * MonchaHub.Size.Y, 0, 1));
            lObject.Closed = true;

            tempList.Add(lObject);

            tempList.OnBaseMesh = false;

            if (tempList.Count > 0)
            {
                MonchaHub.MainFrame = tempList;
                MonchaHub.RefreshFrame();
            }
        }

        public static LObjectList GetPoint(CadObject cadObject)
        {
            LObjectList lObjectList = new LObjectList();

            switch (cadObject.GetType().FullName)
            {
                case "MonchaCadViewer.CanvasObj.CadDot":
                    lObjectList.Add(new LObject(new LPoint3D(cadObject.X, cadObject.Y)));
                    break;
                case "MonchaCadViewer.CanvasObj.CadContour":
                    lObjectList.AddRange(CalcContour(cadObject as CadContour));
                    break;

            }


            return lObjectList;



        }
        public static LObjectList CalcContour(CadContour cadContour)
        {
            LObjectList PathList = new LObjectList();


           TransformGroup transformGroup = cadContour.GmtrObj.Transform as TransformGroup;

            switch (cadContour.GmtrObj.GetType().FullName)
            {
                case "System.Windows.Media.RectangleGeometry":
                    RectangleGeometry rectangleGeometry = (RectangleGeometry)cadContour.GmtrObj;
                    LObject RectangleObject = new LObject();
                    RectangleObject.Closed = true;
                    RectangleObject.Add(new LPoint3D(rectangleGeometry.Rect.X, rectangleGeometry.Rect.Y));
                    RectangleObject.Add(new LPoint3D(rectangleGeometry.Rect.X + rectangleGeometry.Rect.Width, rectangleGeometry.Rect.Y));
                    RectangleObject.Add(new LPoint3D(rectangleGeometry.Rect.X + rectangleGeometry.Rect.Width, rectangleGeometry.Rect.Y + rectangleGeometry.Rect.Height));

                    break;
                case "System.Windows.Media.PathGeometry":
                    pathfigurecalc(cadContour.GmtrObj as PathGeometry);
                    break;
            }

            void pathfigurecalc(PathGeometry pathGeometry)
            {
                foreach (PathFigure figure in pathGeometry.Figures)
                {
                    LObject lObject = new LObject();
                    lObject.Closed = figure.IsClosed;

                    lObject.Add(
                        new LPoint3D(
                            TransPoint(figure.StartPoint)));

                    Point LastPoint = TransPoint(figure.StartPoint);

                    if (figure.Segments.Count > 0)
                    {
                        foreach (PathSegment segment in figure.Segments)
                        {
                            switch (segment.GetType().FullName)
                            {
                                case "System.Windows.Media.BezierSegment":
                                    System.Windows.Media.BezierSegment bezierSegment = (System.Windows.Media.BezierSegment)segment;
                                    lObject.AddRange(
                                        BezieByStep(
                                            LastPoint, TransPoint(bezierSegment.Point1), TransPoint(bezierSegment.Point2), TransPoint(bezierSegment.Point3), cadContour.CRS));
                                    LastPoint = TransPoint(bezierSegment.Point3);
                                    break;
                                case "System.Windows.Media.PolyBezierSegment":
                                    System.Windows.Media.PolyBezierSegment polyBezierSegment = (System.Windows.Media.PolyBezierSegment)segment;
                                    for (int i = 0; i < polyBezierSegment.Points.Count - 2; i += 3)
                                    {
                                        lObject.AddRange(
                                            BezieByStep(
                                                LastPoint, TransPoint(polyBezierSegment.Points[i]), TransPoint(polyBezierSegment.Points[i + 3]), TransPoint(polyBezierSegment.Points[i + 2]), cadContour.CRS));
                                        LastPoint = TransPoint(polyBezierSegment.Points[i + 2]);
                                    }

                                    break;

                                case "System.Windows.Media.LineSegment":
                                    System.Windows.Media.LineSegment lineSegment = (System.Windows.Media.LineSegment)segment;
                                    lObject.Add(
                                        new LPoint3D(
                                            TransPoint(lineSegment.Point)));
                                    LastPoint = TransPoint(lineSegment.Point);
                                    break;

                                case "System.Windows.Media.PolyLineSegment":
                                    System.Windows.Media.PolyLineSegment polyLineSegment = (System.Windows.Media.PolyLineSegment)segment;
                                    for (int i = 0; i < polyLineSegment.Points.Count; i++)
                                    {
                                        lObject.Add(
                                            new LPoint3D(
                                                TransPoint(polyLineSegment.Points[i])));
                                        LastPoint = TransPoint(polyLineSegment.Points.Last());
                                    }
                                    break;

                                case "System.Windows.Media.PolyQuadraticBezierSegment":
                                    System.Windows.Media.PolyQuadraticBezierSegment polyQuadraticBezier = (System.Windows.Media.PolyQuadraticBezierSegment)segment;
                                    for (int i = 0; i < polyQuadraticBezier.Points.Count - 1; i += 2)
                                    {
                                        lObject.AddRange(
                                            QBezierByStep(
                                                LastPoint, TransPoint(polyQuadraticBezier.Points[i]), TransPoint(polyQuadraticBezier.Points[i + 1]), cadContour.CRS));
                                        LastPoint = TransPoint(polyQuadraticBezier.Points[i + 1]);
                                    }
                                    break;

                                case "System.Windows.Media.QuadraticBezierSegment":
                                    System.Windows.Media.QuadraticBezierSegment quadraticBezierSegment = (System.Windows.Media.QuadraticBezierSegment)segment;
                                    lObject.AddRange(
                                        QBezierByStep(
                                            LastPoint, TransPoint(quadraticBezierSegment.Point1), TransPoint(quadraticBezierSegment.Point2), cadContour.CRS));
                                    LastPoint = TransPoint(quadraticBezierSegment.Point2);
                                    break;

                                case "System.Windows.Media.ArcSegment":
                                    System.Windows.Media.ArcSegment arcSegment = (System.Windows.Media.ArcSegment)segment;
                                    lObject.AddRange(
                                        CircleByStep(
                                            LastPoint, TransPoint(arcSegment.Point), arcSegment.Size.Width, arcSegment.SweepDirection, cadContour.CRS, arcSegment.RotationAngle));
                                    LastPoint = TransPoint(arcSegment.Point);

                                    break;
                            }
                        }
                    }
                    if (lObject.Count > 0)
                        PathList.Add(lObject);
                }
            }
            PathList.Optimize();
            Console.WriteLine("Count List " + PathList.GetOnlyPoints.Count);
            return PathList;

            Point TransPoint(Point inPoint)
            {
                transformGroup.TryTransform(inPoint, out Point outPoint);
                return outPoint;
            }
        }

        public static LObject QBezierByStep (Point StartPoint, Point ControlPoint, Point EndPoint, double CRS)
        {
            LPoint3D LastPoint = new LPoint3D(StartPoint);
            double Lenth = 0;
            for (int t = 1; t < 100; t++)
            {
                LPoint3D tempPoint = GetPoint((double)t / 99);
                Lenth += LPoint3D.Lenth(LastPoint, tempPoint);
                LastPoint = tempPoint;
            }

            int CountStep = (int)(Lenth / (CRS)) >= 2 ? (int)(Lenth / CRS) : 2;
            LObject tempObj = new LObject();

            for (int t = 0; t < CountStep; t++)
            {
                tempObj.Add(GetPoint((double)t / (CountStep - 1)));
            }

            return tempObj;

            LPoint3D GetPoint(double t)
            {
                return new LPoint3D(
                    (1 - t) * (1 - t) * StartPoint.X + 2 * (1 - t) * t * ControlPoint.X + t * t * EndPoint.X,
                   (1 - t) * (1 - t) * StartPoint.Y + 2 * (1 - t) * t * ControlPoint.Y + t * t * EndPoint.Y);
            }
        }

        public static LObject BezieByStep(Point point0, Point point1, Point point2, Point point3, double CRS)
        {
            double Lenth = 0;
            LPoint3D LastPoint = new LPoint3D(point1);

            for (int t = 0; t < 100; t++)
            {
                LPoint3D tempPoint = GetPoint((double)t / 99);
                Lenth += LPoint3D.Lenth(LastPoint, tempPoint);
                LastPoint = tempPoint;
            }

            LObject tempObj = new LObject();

            int CountStep = (int)(Lenth / CRS) >= 2 ? (int)(Lenth / CRS) : 2;

            for (int t = 0; t < CountStep; t++)
            {
                tempObj.Add(GetPoint((double)t / (CountStep - 1)));
            }

            return tempObj;

            LPoint3D GetPoint(double t)
            {
                return new LPoint3D(
                    ((1 - t) * (1 - t) * (1 - t)) * point0.X
                           + 3 * ((1 - t) * (1 - t)) * t * point1.X
                           + 3 * (1 - t) * (t * t) * point2.X
                           + (t * t * t) * point3.X,
                    ((1 - t) * (1 - t) * (1 - t)) * point0.Y
                       + 3 * ((1 - t) * (1 - t)) * t * point1.Y
                       + 3 * (1 - t) * (t * t) * point2.Y
                       + (t * t * t) * point3.Y, (byte)1);
            }
        }

        public static LObject CircleByStep(Point StartPoint, Point EndPoint, double radius, SweepDirection clockwise, double CRS, double Delta = 360)
        {
            Delta *= Math.PI / 180;

            //Delta = (Math.PI * 2 + Delta * (clockwise == SweepDirection.Clockwise ? -1 : 1)) % (Math.PI * 2); //to radian

            Point Center = GetCenterArc(StartPoint, EndPoint, radius, clockwise == SweepDirection.Clockwise, Delta > Math.PI && clockwise == SweepDirection.Counterclockwise);

            if (Delta > Math.PI)
             Console.WriteLine("Cntr" + Center);

            double StartAngle = Math.PI * 2 - Math.Atan2(StartPoint.Y - Center.Y, StartPoint.X - Center.X);

            LObject lObject = new LObject();
            double RadianStep = Delta * ((CRS) / (radius * Delta));


            for (double radian = RadianStep; radian <= Delta * 1.005; radian += RadianStep)
            {
                double Angle = (StartAngle + (clockwise == SweepDirection.Counterclockwise ? radian : -radian)) % (2 * Math.PI);

                lObject.Add(new LPoint3D(
                    Center.X + (radius * Math.Cos(Angle)),
                    Center.Y - (radius * Math.Sin(Angle)),
                    1));
            }
            if (lObject.Count > 0)
                lObject.Last().T = 0;

            return lObject;

            Point GetCenterArc(Point StartPt, Point EndPt, double r, bool Clockwise, bool large)
            {
                double radsq = r * r;
                double q = Math.Sqrt(Math.Pow(EndPt.X - StartPt.X, 2) + Math.Pow(EndPt.Y - StartPt.Y, 2));
                double x3 = (StartPt.X + EndPt.X) / 2;
                double y3 = (StartPt.Y + EndPt.Y) / 2;
                double d1 = Math.Sqrt(radsq - ((q / 2) * (q / 2))) * ((StartPt.Y - EndPt.Y) / q) * (large ? -1 : 1);
                double d2 = Math.Sqrt(radsq - ((q / 2) * (q / 2))) * ((EndPt.X - StartPt.X) / q) * (large ? -1 : 1);
                return new Point(
                    x3 + (Clockwise ? d1 : - d1),
                    y3 + (Clockwise ? d2 : - d2)
                    );
            }

        }
    }
}
