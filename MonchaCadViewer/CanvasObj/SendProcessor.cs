using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using AppSt = MonchaCadViewer.Properties.Settings;
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
            lObject.Add(new LPoint3D(device.TTOP.X * MonchaHub.Size.X, device.TBOP.Y * MonchaHub.Size.Y, 0, 1));
            lObject.Add(new LPoint3D(device.TTOP.X * MonchaHub.Size.X, device.TTOP.Y * MonchaHub.Size.Y, 0, 1));
            lObject.Add(new LPoint3D(device.TBOP.X * MonchaHub.Size.X, device.TTOP.Y * MonchaHub.Size.Y, 0, 1));
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
                    lObjectList.Add(new LObject(new LPoint3D(cadObject.Translate.X, cadObject.Translate.Y)));
                    Console.WriteLine("Snd " + cadObject.Translate.X + " " + cadObject.Translate.Y);
                    break;
                case "MonchaCadViewer.CanvasObj.CadContour":
                    lObjectList.AddRange(CalcContour(cadObject.GmtrObj));
                    break;

            }


            return lObjectList;



        }
        public static LObjectList CalcContour(Geometry pg)
        {
            LObjectList PathList = new LObjectList();

            switch (pg.GetType().FullName)
            {
                case "System.Windows.Media.RectangleGeometry":
                    RectangleGeometry rectangleGeometry = (RectangleGeometry)pg;
                    LObject RectangleObject = new LObject();
                    RectangleObject.Closed = true;
                    RectangleObject.Add(new LPoint3D(rectangleGeometry.Rect.X, rectangleGeometry.Rect.Y));
                    RectangleObject.Add(new LPoint3D(rectangleGeometry.Rect.X + rectangleGeometry.Rect.Width, rectangleGeometry.Rect.Y));
                    RectangleObject.Add(new LPoint3D(rectangleGeometry.Rect.X + rectangleGeometry.Rect.Width, rectangleGeometry.Rect.Y + rectangleGeometry.Rect.Height));

                    break;
                case "System.Windows.Media.PathGeometry":
                    pathfigurecalc(pg as PathGeometry);
                    break;
            }

            void pathfigurecalc(PathGeometry pathGeometry)
            {
                foreach (PathFigure figure in pathGeometry.Figures)
                {
                    LObject lObject = new LObject();
                    lObject.Closed = figure.IsClosed;

                    lObject.Add(new LPoint3D(figure.StartPoint));

                    Point LastPoint = figure.StartPoint;

                    if (figure.Segments.Count > 0)
                    {
                        foreach (PathSegment segment in figure.Segments)
                        {
                            switch (segment.GetType().FullName)
                            {
                                case "System.Windows.Media.BezierSegment":
                                    System.Windows.Media.BezierSegment bezierSegment = (System.Windows.Media.BezierSegment)segment;
                                    lObject.AddRange(BezieByStep(LastPoint, bezierSegment.Point1, bezierSegment.Point2, bezierSegment.Point3));
                                    LastPoint = bezierSegment.Point3;
                                    break;
                                case "System.Windows.Media.PolyBezierSegment":
                                    System.Windows.Media.PolyBezierSegment polyBezierSegment = (System.Windows.Media.PolyBezierSegment)segment;
                                    for (int i = 0; i < polyBezierSegment.Points.Count - 2; i += 3)
                                    {
                                        lObject.AddRange(BezieByStep(LastPoint, polyBezierSegment.Points[i], polyBezierSegment.Points[i + 3], polyBezierSegment.Points[i + 2]));
                                        LastPoint = polyBezierSegment.Points[i + 2];
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
                                        lObject.AddRange(QBezierByStep(LastPoint, polyQuadraticBezier.Points[i], polyQuadraticBezier.Points[i + 1]));
                                        LastPoint = polyQuadraticBezier.Points[i + 1];
                                    }

                                    break;

                                case "System.Windows.Media.QuadraticBezierSegment":
                                    System.Windows.Media.QuadraticBezierSegment quadraticBezierSegment = (System.Windows.Media.QuadraticBezierSegment)segment;
                                    lObject.AddRange(QBezierByStep(LastPoint, quadraticBezierSegment.Point1, quadraticBezierSegment.Point2));
                                    LastPoint = quadraticBezierSegment.Point2;
                                    break;

                                case "System.Windows.Media.ArcSegment":
                                    System.Windows.Media.ArcSegment arcSegment = (System.Windows.Media.ArcSegment)segment;
                                    lObject.AddRange(CircleByStep(LastPoint,
                                        arcSegment.Point, arcSegment.Size.Width, arcSegment.SweepDirection, arcSegment.RotationAngle));
                                    LastPoint = arcSegment.Point;

                                    break;
                            }
                        }
                    }
                    if (lObject.Count > 0)
                        PathList.Add(PointsMagic(lObject, pathGeometry));
                }
            }
            PathList.Optimize();
            Console.WriteLine("Count List " + PathList.GetOnlyPoints.Count);
            return PathList;
        }

        public static LObject QBezierByStep (Point StartPoint, Point ControlPoint, Point EndPoint)
        {
            LPoint3D LastPoint = new LPoint3D(StartPoint);
            double Lenth = 0;
            for (int t = 1; t < 100; t++)
            {
                LPoint3D tempPoint = GetPoint((double)t / 99);
                Lenth += LPoint3D.Lenth(LastPoint, tempPoint);
                LastPoint = tempPoint;
            }

            int CountStep = (int)(Lenth / ReadyFrame.CRS.MX) >= 2 ? (int)(Lenth / ReadyFrame.CRS.MX) : 2;
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

        public static LObject BezieByStep(Point point0, Point point1, Point point2, Point point3)
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

            int CountStep = (int)(Lenth / ReadyFrame.CRS.MX) >= 2 ? (int)(Lenth / ReadyFrame.CRS.MX) : 2;

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

        public static LObject CircleByStep(Point StartPoint, Point EndPoint, double radius, SweepDirection clockwise, double Delta = 360)
        {
            Delta = (Math.PI * 2 + Delta * Math.PI / 180 * (clockwise == SweepDirection.Clockwise ? -1 : 1)) % (Math.PI * 2); //to radian

            Point Center = GetCenterArc(StartPoint, EndPoint, radius, clockwise == SweepDirection.Clockwise, Delta > Math.PI);

            if (Delta > Math.PI)
             Console.WriteLine("Cntr" + Center);

            double StartAngle = Math.PI * 2 - Math.Atan2(StartPoint.Y - Center.Y, StartPoint.X - Center.X);

            LObject lObject = new LObject();
            double RadianStep = Delta * (ReadyFrame.CRS.MX / (radius * Delta));


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


        public static LObject PointsMagic(LObject lObject, Geometry geometry)
        {
            if (geometry.Transform is TransformGroup transformGroup)
            {
                TranslateTransform Translate = (TranslateTransform)transformGroup.Children[2];
                RotateTransform Rotate = (RotateTransform)transformGroup.Children[1];
                ScaleTransform Scale = (ScaleTransform)transformGroup.Children[0];

                //зеркалим
                if (Scale.ScaleX == -1)
                {
                    //не влияет на габарит, так как вокруг центра Х
                    pointMirror(lObject);
                }

                //Вращаем
                if (Rotate.Angle != 0)
                {

                    pointRotate(lObject);
                }

                //смещаем
                pointOffcet(lObject);

                //Функция вращения
                LObject pointRotate(LObject _obj)
                {
                    for (int p = 0; p < _obj.Count; p++)
                    {
                        RotatePoint(_obj[p]);
                    }

                    return _obj;

                    void RotatePoint(LPoint3D pointToRotate)
                    {
                        double angleInRadians = Rotate.Angle * ((float)Math.PI / 180);
                        double cosTheta = Math.Cos(angleInRadians);
                        double sinTheta = Math.Sin(angleInRadians);
                        pointToRotate.Set(
                                // X
                                (cosTheta * (pointToRotate.X - Rotate.CenterX) -
                                sinTheta * (pointToRotate.Y - Rotate.CenterY) + Rotate.CenterX),
                                //Y
                                (sinTheta * (pointToRotate.X - Rotate.CenterX) +
                                cosTheta * (pointToRotate.Y - Rotate.CenterY) + Rotate.CenterY),
                                //Z
                                pointToRotate.Z
                            );
                    }
                }

                //Функция смещения
                //Смещает крайнюю нижнюю точку в ноль
                LObject pointOffcet(LObject _obj)
                {
                    for (int i = 0; i < _obj.Count; i++)
                    {
                        _obj[i].X += Translate.X;
                        _obj[i].Y += Translate.Y;
                    }
                    return _obj;
                }

                //Функция отзеркаливания
                LObject pointMirror(LObject _obj)
                {
                    for (int i = 0; i < _obj.Count; i++)
                        _obj[i].X = Translate.X -_obj[i].X;
                    return _obj;
                }
            }

            return lObject;
        }
    }
}
