using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System;
using ToGeometryConverter.Object;
using MonchaSDK.Setting;
using System.Collections.Generic;

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
            Processing = true;
            LObjectList dotList = new LObjectList();
            foreach (object obj in canvas.Children)
            {
                if (obj is CadObject cadObject)
                {
                    if (cadObject.Render)
                    {
                        dotList.AddRange(GetPoint(cadObject, false));
                    }
                }
            }

            LObjectList outList = new LObjectList();
            if (canvas.Masks.Count > 0)
            {
                foreach (CadRectangle cadRectangle in canvas.Masks)
                {
                    outList.AddRange(CutByMask(dotList, cadRectangle));
                }
            }
            else outList = dotList;

            Processing = false;
            if (outList.Count > 0)
            {
                MonchaHub.MainFrame = outList;
                MonchaHub.RefreshFrame();
            }
        }

        /// <summary>
        /// Get point from inner object
        /// </summary>
        /// <param name="cadObject">inner object</param>
        /// <returns>Object collection</returns>
        public static LObjectList GetPoint(CadObject cadObject, bool InGroup)
        {
            LObjectList lObjectList = new LObjectList();

            switch (cadObject)
            {
                case CadAnchor cadDot:
                    if (cadObject.DataContext is MonchaDeviceMesh deviceMesh)
                    {
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_Dot) lObjectList.Add(new LObject() { cadDot.GetPoint.GetMLpoint3D });
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_Rect) lObjectList.AddRange(CalibrationRect(deviceMesh, cadDot.GetPoint));
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_Cross) lObjectList.AddRange(CalibrationCross(deviceMesh, cadDot.GetPoint));
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_HLine) lObjectList.AddRange(CalibrationLineH(deviceMesh, cadDot.GetPoint));
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_WLine) lObjectList.AddRange(CalibrationLineW(deviceMesh, cadDot.GetPoint));

                        lObjectList.NoMesh = true;
                    }
                    else
                    {
                        lObjectList.Add(new LObject()
                        {
                            cadDot.GetPoint.GetMLpoint3D
                        });
                    }

                    break;
                case CadContour cadContour:
                    lObjectList.AddRange(CalcContour(cadContour));
                    if (InGroup == false) lObjectList.Transform(cadObject.TransformGroup);
                    break;
                case CadObjectsGroup cadObjectsGroup:
                    foreach (CadObject obj in cadObjectsGroup.cadObjects)
                    {
                        lObjectList.AddRange(GetPoint(obj, true));
                    }
                    lObjectList.Transform(cadObjectsGroup.TransformGroup);
                    break;

            }

            return lObjectList;
            

            LObjectList CalibrationCross(MonchaDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;


                //Vertical
                LObject Line1 = new LObject();

                for (int i = 0; i <= width; i += 1)
                {
                    Line1.Add(monchaDeviceMesh[tuple.Item2, i].GetMLpoint3D);
                }

                //Vertical

                //Horizontal
                LObject Line2 = new LObject();

                for (int i = 0; i <= height; i += 1)
                {
                    Line2.Add(monchaDeviceMesh[i, tuple.Item1].GetMLpoint3D);
                }

                return new LObjectList()
            {
                Line2,
                Line1

            };

            }
            LObjectList CalibrationRect(MonchaDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;

                //Vertical
                LObject Line1H = GetLine(tuple.Item1, tuple.Item2, true);
                //Horizontal
                LObject Line1W = GetLine(tuple.Item1, height - tuple.Item2, false);
                //Vertical
                LObject Line2H = GetLine(width - tuple.Item1, height - tuple.Item2, true);
                //Horizontal
                LObject Line2W = GetLine(width - tuple.Item1, tuple.Item2, false);

                return new LObjectList()
            {
                Line1H,
                Line1W,
                Line2H,
                Line2W
            };


                LObject GetLine(int xpos, int ypos, bool Vertical)
                {
                    LObject Line = new LObject();

                    if (Vertical == true)
                    {
                        int ypos2 = height - ypos;

                        int delta = ypos > ypos2 ? -1 : 1;

                        for (int i = 0; i <= Math.Abs(ypos2 - ypos); i += 1)
                        {
                            Line.Add(monchaDeviceMesh[ypos + (i * delta), xpos].GetMLpoint3D);
                        }
                    }
                    else
                    {
                        int xpos2 = width - xpos;

                        int delta = xpos > xpos2 ? -1 : 1;

                        for (int i = 0; i <= Math.Abs(xpos2 - xpos); i += 1)
                        {
                            Line.Add(monchaDeviceMesh[ypos, xpos + (i * delta)].GetMLpoint3D);
                        }
                    }

                    return Line;
                }

            }
            LObjectList CalibrationLineH(MonchaDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;


                //Height
                LObject Line = new LObject();

                for (int i = 0; i <= height; i += 1)
                {
                    Line.Add(monchaDeviceMesh[i, tuple.Item1].GetMLpoint3D);
                }

                return new LObjectList()
            {
                Line,
            };

            }
            LObjectList CalibrationLineW(MonchaDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;

                //width
                LObject Line = new LObject();

                for (int i = 0; i <= width; i += 1)
                {
                    Line.Add(monchaDeviceMesh[tuple.Item2, i].GetMLpoint3D);
                }



                return new LObjectList()
            {
                Line
            };

            }
        }



        /// <summary>
        /// Convert inner object in LPoint3D's
        /// </summary>
        public static LObjectList CalcContour(CadObject cadObject)
        {
            LObjectList PathList = new LObjectList();

            switch ((object)cadObject)
            {
                case NurbsShape nurbsShape:
                    LObject NurbsObject = new LObject();
                    foreach (Point nurbsPoint in nurbsShape.BSplinePoints(cadObject.ProjectionSetting.PointStep.MX))
                    {
                        NurbsObject.Add(new LPoint3D(nurbsPoint));
                    }
                    NurbsObject.ProjectionSetting = cadObject.ProjectionSetting;
                    PathList.Add(NurbsObject);
                    break;

                case CadContour cadContour:
                    if (cadContour.myGeometry is PathGeometry pathGeometry)
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
                            switch (segment)
                            {
                                case BezierSegment bezierSegment:
                                    lObject.AddRange(
                                        BezieByStep(
                                            LastPoint, bezierSegment.Point1, bezierSegment.Point2, bezierSegment.Point3, projectionSetting.PointStep.MX));
                                    LastPoint = bezierSegment.Point3;
                                    break;

                                case PolyBezierSegment polyBezierSegment:
                                    for (int i = 0; i < polyBezierSegment.Points.Count - 2; i += 3)
                                    {
                                        lObject.AddRange(
                                            BezieByStep(
                                                LastPoint, polyBezierSegment.Points[i], polyBezierSegment.Points[i + 2], polyBezierSegment.Points[i + 1], projectionSetting.PointStep.MX));
                                        LastPoint = polyBezierSegment.Points[i + 1];
                                    }
                                    break;

                                case LineSegment lineSegment:
                                    lObject.Add(new LPoint3D(lineSegment.Point));
                                    LastPoint = lineSegment.Point;
                                    break;

                                case PolyLineSegment polyLineSegment:
                                    for (int i = 0; i < polyLineSegment.Points.Count; i++)
                                    {
                                        lObject.Add(new LPoint3D(polyLineSegment.Points[i]));
                                        LastPoint = polyLineSegment.Points.Last();
                                    }
                                    break;

                                case PolyQuadraticBezierSegment polyQuadraticBezier:
                                    for (int i = 0; i < polyQuadraticBezier.Points.Count - 1; i += 2)
                                    {
                                        lObject.AddRange(
                                            QBezierByStep(
                                                LastPoint, polyQuadraticBezier.Points[i], polyQuadraticBezier.Points[i + 1], projectionSetting.PointStep.MX));
                                        LastPoint = polyQuadraticBezier.Points[i + 1];
                                    }
                                    break;

                                case QuadraticBezierSegment quadraticBezierSegment:
                                    lObject.AddRange(
                                        QBezierByStep(
                                            LastPoint, quadraticBezierSegment.Point1, quadraticBezierSegment.Point2, projectionSetting.PointStep.MX));
                                    LastPoint = quadraticBezierSegment.Point2;
                                    break;

                                case ArcSegment arcSegment:
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

                double koeff = (radius / radiusEdge) < 0.3 ? 0.3 : (radius / radiusEdge);
                koeff = (radius / radiusEdge) > 3 ? 3 : (radius / radiusEdge);

                double RadianStep = Delta / (int)((Delta * radius) / CRS);

                for (double radian = 0; radian <= Delta * 1.005; radian += RadianStep)
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


        public static LObjectList CutByMask(LObjectList Contour, CadRectangle Mask)
        {
            LObjectList CutObjects = new LObjectList();

            CutObjects.NoMesh = Contour.NoMesh;

            for (int j = 0; j < Contour.Count; j++)
            {

                CutObjects.Add(new LObject());
                CutObjects.Last().Closed = Contour[j].Closed;

                if (Contour[j].Count > 1)
                {
                    for (int i = 0; i < Contour[j].Count - 1; i++)
                    {
                        CheckLine(Contour[j][i], Contour[j][i + 1], Contour[j][i + 1] == Contour[j].Last(), CutObjects);
                    }

                    if (Contour[j].Closed && !CutObjects.Last().Closed)
                        CheckLine(Contour[j].Last(), Contour[j].First(), true, CutObjects);

                    if (CutObjects.Last().Count < 1)
                        CutObjects.Remove(CutObjects.Last());
                }
                else
                {
                    if (Contour[j].Count > 0)
                    {
                        if (Mask.CheckInDot(Contour[j][0]))
                        {
                            CutObjects.Last().Add(Contour[j][0]);
                        }
                        else
                            CutObjects.Remove(CutObjects.Last());
                    }
                }

                if (CutObjects.Count > 0)
                    if (CutObjects.Last().Count < 1) CutObjects.Remove(CutObjects.Last());

            } 
            return CutObjects;

            void CheckLine(LPoint3D point1, LPoint3D point2, bool last, LObjectList objectList)
            {
                List<LPoint3D> CrossPoint = new List<LPoint3D>();
                //Если point1 внутри
                if (Mask.CheckInDot(point1))
                {
                    CrossPoint.Add(point1);
                }
                else
                {
                    if (objectList.Last().Count > 0)
                    {
                        objectList.Last().Closed = false;
                        objectList.Add(new LObject());
                        objectList.Last().Closed = false;
                    }
                    else if (objectList.Last().Count > 0)
                    {
                        objectList.Last().Closed = false;
                    }
                }

                //Если какая то из точек выходит > TOP || < BOP
                if (!Mask.CheckInDot(point1) || !Mask.CheckInDot(point2))
                {
                    //Ищем пересечения
                    CrossPoint.AddRange(PointCross(point1, point2));
                }

                //если point2 внутри
                if (Mask.CheckInDot(point2) && last)
                {
                    //Если point2 внутри
                    CrossPoint.Add(point2);
                }
                else if (!Mask.CheckInDot(point2))
                {
                    objectList.Last().Closed = false;
                }

                if (CrossPoint.Count > 0)
                {
                    objectList.Last().AddRange(CrossPoint);
                }
            }

            List<LPoint3D> PointCross(LPoint3D point1, LPoint3D point2)
            {
                List<LPoint3D> ListIntersection = new List<LPoint3D>();

                Point[] edgeList = new Point[4]
                {
                    Mask.Bounds.TopLeft,
                    Mask.Bounds.TopRight,
                    Mask.Bounds.BottomRight,
                    Mask.Bounds.BottomLeft
                };

                for (int k = 0; k < edgeList.Length; k++)
                {
                    LPoint3D intersection;

                    if (FindIntersection(point1.GetMPoint, point2.GetMPoint, edgeList[k], edgeList[(k + 1) % edgeList.Length], out intersection))
                        ListIntersection.Add(intersection);
                }
                return ListIntersection;
            }

            bool FindIntersection(
            System.Windows.Point p1, System.Windows.Point p2, System.Windows.Point p3, System.Windows.Point p4,
            out LPoint3D intersection)
            {
                // Get the segments' parameters.
                double dx12 = p2.X - p1.X;
                double dy12 = p2.Y - p1.Y;
                double dx34 = p4.X - p3.X;
                double dy34 = p4.Y - p3.Y;

                // Solve for t1 and t2
                double denominator = (dy12 * dx34 - dx12 * dy34);

                double t1 =
                    ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
                        / denominator;
                if (double.IsInfinity(t1))
                {
                    // The lines are parallel (or close enough to it).
                    intersection = new LPoint3D(double.NaN, double.NaN);
                    return false;
                }

                double t2 =
                    ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
                        / -denominator;

                // Find the point of intersection.
                intersection = new LPoint3D(p1.X + dx12 * t1, p1.Y + dy12 * t1);

                // The segments intersect if t1 and t2 are between 0 and 1.
                return ((t1 >= 0) && (t1 <= 1) &&
                     (t2 >= 0) && (t2 <= 1));

            }
        }
    }
}
