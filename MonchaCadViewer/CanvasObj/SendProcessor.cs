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
using System.Threading.Tasks;
using System.Windows.Threading;

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
                foreach (LRect lRect in canvas.Masks)
                {
                   outList.AddRange(lRect.CutByRect(dotList));
                }
            }
            else outList = dotList;

            Processing = false;

            MonchaHub.MainFrame = outList;
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
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_Dot) lObjectList.Add(new LObject() { Points = new List<LPoint3D>() { cadDot.GetPoint.GetMLpoint3D }, MeshType = cadDot.MeshType });
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_Rect) lObjectList.AddRange(CalibrationRect(deviceMesh, cadDot.GetPoint, cadDot.MeshType));
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_miniRect) lObjectList.AddRange(CalibrationMiniRect(deviceMesh, cadDot.GetPoint, cadDot.MeshType));
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_Cross) lObjectList.AddRange(CalibrationCross(deviceMesh, cadDot.GetPoint, cadDot.MeshType));
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_HLine) lObjectList.AddRange(CalibrationLineH(deviceMesh, cadDot.GetPoint, cadDot.MeshType));
                        if (MonchaDeviceMesh.ClbrForm == CalibrationForm.cl_WLine) lObjectList.AddRange(CalibrationLineW(deviceMesh, cadDot.GetPoint, cadDot.MeshType));
                    }
                    else
                    {
                        lObjectList.Add(new LObject()
                        {
                            Points = new List<LPoint3D>()
                            {
                                cadDot.GetPoint.GetMLpoint3D
                            },
                            MeshType = cadDot.MeshType
                        });
                    }

                    break;
                case CadContour cadContour:
                    lObjectList.AddRange(CalcContour(cadContour));
                    if (InGroup == false) lObjectList.Transform(cadObject.TransformGroup);
                    break;
                case CadLine cadLine:
                    lObjectList.Add(new LObject()
                    {
                        Points = new List<LPoint3D>() {
                        cadLine.P1,
                        cadLine.P2
                        },
                        ProjectionSetting = cadLine.ProjectionSetting,
                        MeshType = cadLine.MeshType
                    });
                    break;
                case CadRectangle cadRectangle:
                    lObjectList.Add(new LObject()
                    {
                        Points = new List<LPoint3D>() {
                        cadRectangle.LRect.P1,
                        new LPoint3D(cadRectangle.LRect.P2.MX, cadRectangle.LRect.P1.MY),
                        cadRectangle.LRect.P2,
                        new LPoint3D(cadRectangle.LRect.P1.MX, cadRectangle.LRect.P2.MY),
                        },
                        ProjectionSetting = cadRectangle.ProjectionSetting,
                        Closed = true,
                        MeshType = cadRectangle.MeshType
                    });
                    break;
                case CadObjectsGroup cadObjectsGroup:
                    foreach (CadObject obj in cadObjectsGroup)
                    {
                        obj.ProjectionSetting = cadObjectsGroup.ProjectionSetting != MonchaHub.ProjectionSetting ? cadObjectsGroup.ProjectionSetting : null;
                        lObjectList.AddRange(GetPoint(obj, true));
                    }
                    lObjectList.Transform(cadObjectsGroup.TransformGroup);
                    break;

            }

            return lObjectList;
            

            LObjectList CalibrationCross(MonchaDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D, MeshType meshType)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;


                //Vertical
                LObject Line1 = new LObject()
                {
                    MeshType = meshType
                };

                for (int i = 0; i <= width; i += 1)
                {
                    Line1.Add(monchaDeviceMesh[tuple.Item2, i].GetMLpoint3D);
                }

                //Vertical

                //Horizontal
                LObject Line2 = new LObject()
                {
                    MeshType = meshType
                };

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
            LObjectList CalibrationRect(MonchaDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D, MeshType meshType)
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
                    LObject Line = new LObject()
                    {
                        MeshType = meshType
                    };

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
            LObjectList CalibrationMiniRect(MonchaDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D, MeshType meshType)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;

                return new LObjectList()
                {
                        new LObject(){
                            Points = new List<LPoint3D>(){
                                monchaDeviceMesh[tuple.Item2, tuple.Item1].GetMLpoint3D,
                                monchaDeviceMesh[tuple.Item2, tuple.Item1 + (tuple.Item1 < width ? 1 : -1)].GetMLpoint3D,
                                monchaDeviceMesh[tuple.Item2 + (tuple.Item2 < height ? 1 : -1), tuple.Item1 + (tuple.Item1 < width ? 1 : -1)].GetMLpoint3D,
                                monchaDeviceMesh[tuple.Item2 + (tuple.Item2 < height ? 1 : -1), tuple.Item1].GetMLpoint3D
                            },
                            Closed = true,
                            MeshType = meshType
                        }
                };

            }
            LObjectList CalibrationLineH(MonchaDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D, MeshType meshType)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;


                //Height
                LObject Line = new LObject()
                {
                    MeshType = meshType
                };

                for (int i = 0; i <= height; i += 1)
                {
                    Line.Add(monchaDeviceMesh[i, tuple.Item1].GetMLpoint3D);
                }

                return new LObjectList()
            {
                Line,
            };

            }
            LObjectList CalibrationLineW(MonchaDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D, MeshType meshType)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;

                //width
                LObject Line = new LObject()
                {
                    MeshType = meshType
                };

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
                    LObject NurbsObject = new LObject()
                    {
                        MeshType = cadObject.MeshType
                    };
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
                    else if (cadContour.myGeometry is EllipseGeometry ellipseGeometry)
                    {
                        LObject ellipseObj = new LObject()
                        {
                            ProjectionSetting = cadObject.ProjectionSetting,
                            MeshType = cadContour.MeshType
                        };

                        double C_x = ellipseGeometry.Center.X, C_y = ellipseGeometry.Center.Y, w = ellipseGeometry.RadiusX, h = ellipseGeometry.RadiusY;

                        double step = (Math.PI * 2) / ((Math.PI * (w + h)) / cadObject.ProjectionSetting.PointStep.MX);

                        for (double t = 0; t <= 2 * Math.PI; t += step)
                        {
                            ellipseObj.Add(new LPoint3D()
                            {
                                X = C_x + (w / 2) * Math.Cos(t),
                                Y = C_y + (h / 2) * Math.Sin(t)
                        });
                        }
                        PathList.Add(ellipseObj);
                    }
                    else if (cadContour.myGeometry is LineGeometry lineGeometry)
                    {
                        PathList.Add(new LObject()
                        {
                            Points = new List<LPoint3D>()
                            { 
                                new LPoint3D(lineGeometry.StartPoint),
                                new LPoint3D(lineGeometry.EndPoint) 
                            },
                            ProjectionSetting = cadContour.ProjectionSetting
                        });
                    }
                    break;
                default:
                    
                    break;
            }

            return PathList;


            LObjectList pathfigurecalc(PathGeometry pathGeometry, LProjectionSetting projectionSetting)
            {
                LObjectList PathObjectList = new LObjectList();

                foreach (PathFigure figure in pathGeometry.Figures)
                {
                    LObject lObject = new LObject()
                    {
                        MeshType = cadObject.MeshType
                    };
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
                                default:
                                    Console.WriteLine($"Unkom type segment: {segment.GetType().Name}");
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
    }
}
