using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System;
using ToGeometryConverter.Object;
using System.Windows.Shapes;
using MonchaSDK.Setting;

namespace MonchaCadViewer.CanvasObj
{
    public static class SendProcessor
    {
        public static bool Processing = false;

        /// <summary>
        /// Обрабатывает объекты и готовит их на лазер
        /// </summary>
        /// <param name="canvas">Отправляемое рабочее поле</param>
        /// <returns>N_{i,degree}(step)</returns>
        public static void Worker(CadCanvas canvas)
        {
            Console.WriteLine("Worker");

                Processing = true;
                LObjectList tempList = new LObjectList();
                foreach (object obj in canvas.Children)
                {
                    if (obj is CadObject cadObject)
                    {
                        if (cadObject.Render)
                        {
                            tempList.AddRange(GetPoint(cadObject));
                        }

                        tempList.OnBaseMesh = cadObject.OnBaseMesh;
                    }
                }

                if (tempList.Count > 0)
                {
                    MonchaHub.MainFrame = tempList;
                    MonchaHub.RefreshFrame();
                }
                Processing = false;
            
        }



        /// <summary>
        /// Get point from inner object
        /// </summary>
        /// <param name="cadObject">inner object</param>
        /// <returns>Object collection</returns>
        public static LObjectList GetPoint(CadObject cadObject)
        {
            LObjectList lObjectList = new LObjectList();

            switch (cadObject.GetType().FullName)
            {
                case "MonchaCadViewer.CanvasObj.CadDot":
                    CadDot cadDot = (CadDot)cadObject;
                    if (cadObject.DataContext is MonchaDeviceMesh deviceMesh)
                    {
                       Tuple<int, int> tuple = deviceMesh.CoordinatesOf(cadDot.Point);
                        int height = deviceMesh.GetLength(0) - 1;
                        int width = deviceMesh.GetLength(1) - 1;
                        LObject MeshRectangle = new LObject();

                        MeshRectangle.Add(deviceMesh[tuple.Item2, tuple.Item1].GetMLpoint3D);
                        int delta = tuple.Item2 <= (height - tuple.Item2) ? 1 : -1;

                        for (int i = delta; Math.Abs(i) <= Math.Abs(height - tuple.Item2 * 2); i += delta)
                        {
                            MeshRectangle.Add(deviceMesh[tuple.Item2 + i, tuple.Item1].GetMLpoint3D);
                        }

                        MeshRectangle.Add(deviceMesh[height - tuple.Item2 , width - tuple.Item1].GetMLpoint3D);
                        delta = tuple.Item2 > (height - tuple.Item2) ? 1 : -1;

                        for (int i = delta; Math.Abs(i) <= Math.Abs(height - tuple.Item2 * 2); i += delta)
                        {
                            MeshRectangle.Add(deviceMesh[height - tuple.Item2 + i, width - tuple.Item1].GetMLpoint3D);
                        }

                        MeshRectangle.Closed = true;
                        lObjectList.Add(MeshRectangle);
                        lObjectList.NoMesh = true;
                    }
                    else
                    {
                        lObjectList.Add(new LObject()
                        {
                            cadDot.Point.GetMLpoint3D
                        });
                    }

                    break;
                case "MonchaCadViewer.CanvasObj.CadContour":
                    lObjectList.AddRange(CalcContour(cadObject as CadContour));
                    break;

            }
            if (cadObject.OnBaseMesh == true)
            {
                return lObjectList;
            }
            else
            {
                return lObjectList.Transform(cadObject.Transform);
            }
        }

        /// <summary>
        /// Convert inner object in LPoint3D's
        /// </summary>
        public static LObjectList CalcContour(CadObject cadObject)
        {
            LObjectList PathList = new LObjectList();

            switch (cadObject.ObjectShape.GetType().Name)
            {
                case "Rectangle":
                    break;

                case "NurbsShape":
                    NurbsShape nurbsShape = (NurbsShape)cadObject.ObjectShape;
                    LObject NurbsObject = new LObject();
                    foreach (Point nurbsPoint in nurbsShape.BSplinePoints(cadObject.ProjectionSetting.PointStep.MX))
                    {
                        NurbsObject.Add(new LPoint3D(nurbsPoint));
                    }
                    NurbsObject.ProjectionSetting = cadObject.ProjectionSetting;
                    PathList.Add(NurbsObject);

                    break;
                case "Path":
                    Path path = (Path)cadObject.ObjectShape;
                    if (path.Data is PathGeometry pathGeometry)
                    {
                        PathList.AddRange(pathfigurecalc(pathGeometry, cadObject.ProjectionSetting));
                    }

                    break;
            }

            return PathList;


            LObjectList pathfigurecalc(PathGeometry pathGeometry, LProjectionSetting projectionSetting)
            {
                LObjectList PathObjectList = new LObjectList();

                foreach (PathFigure figure in pathGeometry.Figures)
                {
                    LObject lObject = new LObject();
                    lObject.ProjectionSetting = cadObject.ProjectionSetting;
                    lObject.Closed = figure.IsClosed;

                    lObject.Add(
                        new LPoint3D(
                            figure.StartPoint));

                    Point LastPoint = figure.StartPoint;

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
                                            LastPoint, bezierSegment.Point1, bezierSegment.Point2, bezierSegment.Point3, projectionSetting.PointStep.MX));
                                    LastPoint = bezierSegment.Point3;
                                    break;
                                case "System.Windows.Media.PolyBezierSegment":
                                    System.Windows.Media.PolyBezierSegment polyBezierSegment = (System.Windows.Media.PolyBezierSegment)segment;
                                    for (int i = 0; i < polyBezierSegment.Points.Count - 2; i += 3)
                                    {
                                        lObject.AddRange(
                                            BezieByStep(
                                                LastPoint, polyBezierSegment.Points[i], polyBezierSegment.Points[i + 2], polyBezierSegment.Points[i + 1], projectionSetting.PointStep.MX));
                                        LastPoint = polyBezierSegment.Points[i + 1];
                                    }

                                    break;

                                case "System.Windows.Media.LineSegment":
                                    System.Windows.Media.LineSegment lineSegment = (System.Windows.Media.LineSegment)segment;
                                    lObject.Add(new LPoint3D(lineSegment.Point));
                                    LastPoint = lineSegment.Point;
                                    break;

                                case "System.Windows.Media.PolyLineSegment":
                                    System.Windows.Media.PolyLineSegment polyLineSegment = (System.Windows.Media.PolyLineSegment)segment;
                                    for (int i = 0; i < polyLineSegment.Points.Count; i++)
                                    {
                                        lObject.Add(new LPoint3D(polyLineSegment.Points[i]));
                                        LastPoint = polyLineSegment.Points.Last();
                                    }
                                    break;

                                case "System.Windows.Media.PolyQuadraticBezierSegment":
                                    System.Windows.Media.PolyQuadraticBezierSegment polyQuadraticBezier = (System.Windows.Media.PolyQuadraticBezierSegment)segment;
                                    for (int i = 0; i < polyQuadraticBezier.Points.Count - 1; i += 2)
                                    {
                                        lObject.AddRange(
                                            QBezierByStep(
                                                LastPoint, polyQuadraticBezier.Points[i], polyQuadraticBezier.Points[i + 1], projectionSetting.PointStep.MX));
                                        LastPoint = polyQuadraticBezier.Points[i + 1];
                                    }
                                    break;

                                case "System.Windows.Media.QuadraticBezierSegment":
                                    System.Windows.Media.QuadraticBezierSegment quadraticBezierSegment = (System.Windows.Media.QuadraticBezierSegment)segment;
                                    lObject.AddRange(
                                        QBezierByStep(
                                            LastPoint, quadraticBezierSegment.Point1, quadraticBezierSegment.Point2, projectionSetting.PointStep.MX));
                                    LastPoint = quadraticBezierSegment.Point2;
                                    break;

                                case "System.Windows.Media.ArcSegment":
                                    System.Windows.Media.ArcSegment arcSegment = (System.Windows.Media.ArcSegment)segment;

                                    SweepDirection sweepDirection = arcSegment.SweepDirection;

                                    if (cadObject.Mirror)
                                    {
                                        sweepDirection = arcSegment.SweepDirection == SweepDirection.Clockwise ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;
                                    }

                                    foreach (Point lPoint3D in CircleByStep(
                                            LastPoint, arcSegment.Point, arcSegment.Size.Width, projectionSetting.RadiusEdge, sweepDirection, projectionSetting.PointStep.MX, arcSegment.RotationAngle))
                                    {
                                        lObject.Add(new LPoint3D(lPoint3D));
                                    }
                                    LastPoint = arcSegment.Point;
                                    break;
                            }
                        }
                    }
                    if (lObject.Count > 0)
                        PathObjectList.Add(lObject);
                }
                return PathObjectList;
            }
        }



        /// <summary>
        /// interpolation Qbezier
        /// </summary>
        public static LObject QBezierByStep(Point StartPoint, Point ControlPoint, Point EndPoint, double CRS)
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

        /// <summary>
        /// interpolation bezier
        /// </summary>
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

        /// <summary>
        /// interpolation Circle or arc
        /// </summary>
        public static PointCollection CircleByStep(Point StartPoint, Point EndPoint, double radius, double radiusEdge, SweepDirection clockwise, double CRS, double Delta = 360)
        {
            Delta *= Math.PI / 180;

            PointCollection lObject = new PointCollection();

            if (Delta != 0)
            {
                Point Center = GetCenterArc(StartPoint, EndPoint, radius, clockwise == SweepDirection.Clockwise, Delta > Math.PI && clockwise == SweepDirection.Counterclockwise);

                double StartAngle = Math.PI * 2 - Math.Atan2(StartPoint.Y - Center.Y, StartPoint.X - Center.X);

                double koeff = (radius / radiusEdge) > 3 ? 3 : (radius / radiusEdge);
                koeff = (radius / radiusEdge) < 0.3 ? 0.3 : (radius / radiusEdge);

                double RadianStep = Delta * (CRS) / (radius * Delta) * koeff;

                for (double radian = RadianStep; radian <= Delta * 1.005; radian += RadianStep)
                {
                    double Angle = (StartAngle + (clockwise == SweepDirection.Counterclockwise ? radian : -radian)) % (2 * Math.PI);

                    lObject.Add(new Point(
                        Center.X + (radius * Math.Cos(Angle)),
                        Center.Y - (radius * Math.Sin(Angle))
                        ));
                }
            }
            else
            {
                if (clockwise == SweepDirection.Counterclockwise)
                {
                    lObject.Add(StartPoint);
                    lObject.Add(EndPoint);
                }
                else
                {
                    lObject.Add(EndPoint);
                    lObject.Add(StartPoint);
                }

            }

            return lObject;

            Point GetCenterArc(Point StartPt, Point EndPt, double r, bool Clockwise, bool large)
            {
                double radsq = r * r;
                double q = Math.Sqrt(Math.Pow(EndPt.X - StartPt.X, 2) + Math.Pow(EndPt.Y - StartPt.Y, 2));
                double x3 = (StartPt.X + EndPt.X) / 2;
                double y3 = (StartPt.Y + EndPt.Y) / 2;
                double d1 = 0;
                double d2 = 0;
                if (radsq > 0)
                {
                    d1 = Math.Sqrt(radsq - ((q / 2) * (q / 2))) * ((StartPt.Y - EndPt.Y) / q) * (large ? -1 : 1);
                    d2 = Math.Sqrt(radsq - ((q / 2) * (q / 2))) * ((EndPt.X - StartPt.X) / q) * (large ? -1 : 1);
                }
                return new Point(
                    x3 + (Clockwise ? d1 : -d1),
                    y3 + (Clockwise ? d2 : -d2)
                    );
            }

        }
    }
}
