using CadProjectorSDK;
using CadProjectorSDK.Device;
using CadProjectorSDK.Object;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System;
using ToGeometryConverter.Object;
using CadProjectorSDK.Setting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;
using ToGeometryConverter.Object.Elements;

namespace CadProjectorViewer.CanvasObj
{
    public static class SceneSender
    {
        public static bool Processing = false;

        /// <summary>
        /// Обрабатывает объекты и готовит их на лазер
        /// </summary>
        /// <param name="canvas">Отправляемое рабочее поле</param>
        /// <returns>N_{i,degree}(step)</returns>
        public async static Task<LObjectList> GetLObject (ProjectionScene scene)
        {
            Processing = true;
            LObjectList dotList = new LObjectList();

            foreach (object obj in scene.Objects)
            {
                if (obj is CadObject cadObject && cadObject.Render == true)
                {
                    dotList.AddRange(SceneSender.GetPoint(cadObject, false));
                    //dotList.AddRange(cadObject.GetTransformPoint(false));
                }
            }
            LObjectList outList = new LObjectList();
            if (scene.Masks.Count > 0)
            {
                foreach (CadRectangle rectangle in scene.Masks)
                {
                   outList.AddRange(await rectangle.LRect.GetCutObjects(dotList));
                }
            }
            else outList = dotList;

            Processing = false;

            return outList;
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
                    if (cadObject.DataContext is LDeviceMesh deviceMesh)
                    {
                        if (LDeviceMesh.ClbrForm == CalibrationForm.cl_Dot) 
                            lObjectList.Add(new LObject() { Points = new List<LPoint3D>() { cadDot.GetLPoint.GetMLpoint3D }, MeshType = cadObject.MeshType });
                        if (LDeviceMesh.ClbrForm == CalibrationForm.cl_Rect) 
                            lObjectList.AddRange(CalibrationRect(deviceMesh, cadDot.GetLPoint));
                        if (LDeviceMesh.ClbrForm == CalibrationForm.cl_miniRect) 
                            lObjectList.AddRange(CalibrationMiniRect(deviceMesh, cadDot.GetLPoint));
                        if (LDeviceMesh.ClbrForm == CalibrationForm.cl_Cross) 
                            lObjectList.AddRange(CalibrationCross(deviceMesh, cadDot.GetLPoint));
                        if (LDeviceMesh.ClbrForm == CalibrationForm.cl_HLine) 
                            lObjectList.AddRange(CalibrationLineH(deviceMesh, cadDot.GetLPoint));
                        if (LDeviceMesh.ClbrForm == CalibrationForm.cl_WLine) 
                            lObjectList.AddRange(CalibrationLineW(deviceMesh, cadDot.GetLPoint));
                    }
                    else
                    {
                        lObjectList.Add(new LObject()
                        {
                            Points = new List<LPoint3D>()
                            {
                                cadDot.GetLPoint.GetMLpoint3D
                            },
                            MeshType = MeshType.NONE
                        });
                    }

                    break;
                case CadGeometry cadGeometry:
                    lObjectList.AddRange(cadGeometry.GetTransformPoints());
                    break;
                case CadLine cadLine:
                    lObjectList.Add(new LObject()
                    {
                        Points = new List<LPoint3D>() {
                        cadLine.P1,
                        cadLine.P2
                        },
                        ProjectionSetting = cadLine.ProjectionSetting
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
                        Closed = true
                    });
                    break;
                case CadObjectsGroup cadObjectsGroup:
                    foreach (CadObject obj in cadObjectsGroup)
                    {
                        if (obj.Render == true)
                        {
                            obj.ProjectionSetting = cadObjectsGroup.ProjectionSetting != ProjectorHub.ProjectionSetting ? cadObjectsGroup.ProjectionSetting : null;
                            lObjectList.AddRange(GetPoint(obj, true));
                        }
                    }
                    break;

            }

            return lObjectList;

            #region GetMeshShape
            LObjectList CalibrationCross(LDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D)
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
            LObjectList CalibrationRect(LDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D)
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
                    LObject Line = new LObject() { MeshType = cadObject.MeshType };

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
            LObjectList CalibrationMiniRect(LDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D)
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
                            MeshType = cadObject.MeshType
                        }
                };

            }
            LObjectList CalibrationLineH(LDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;


                //Height
                LObject Line = new LObject() { MeshType = cadObject.MeshType };

                for (int i = 0; i <= height; i += 1)
                {
                    Line.Add(monchaDeviceMesh[i, tuple.Item1].GetMLpoint3D);
                }

                return new LObjectList()
                {
                    Line,
                };

            }
            LObjectList CalibrationLineW(LDeviceMesh monchaDeviceMesh, LPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;

                //width
                LObject Line = new LObject() { MeshType = cadObject.MeshType }; ;

                for (int i = 0; i <= width; i += 1)
                {
                    Line.Add(monchaDeviceMesh[tuple.Item2, i].GetMLpoint3D);
                }

                return new LObjectList()
                {
                    Line
                };

            }
            #endregion
        }

        /// <summary>
        /// Convert inner object in LPoint3D's
        /// </summary>
        public async static  Task<LObjectList> CalcContour(CadObject cadObject)
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

                case CadGeometry cadContour:

                    foreach(PointsElement pntobj in cadContour.GCObject.GetPointCollection(cadContour.TransformGroup, cadObject.ProjectionSetting.PointStep.MX, cadObject.ProjectionSetting.RadiusEdge))
                    {
                        PathList.Add(new LObject(pntobj.GetPoints3D));
                    }

                    break;
                default:
                    
                    break;
            }

            return PathList;
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
                Lenth += LPoint3D.Lenth2D(LastPoint, tempPoint);
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
                Lenth += LPoint3D.Lenth2D(LastPoint, tempPoint);
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
