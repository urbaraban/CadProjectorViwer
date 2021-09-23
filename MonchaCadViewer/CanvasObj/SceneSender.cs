using CadProjectorSDK;
using CadProjectorSDK.Device;
using CadProjectorSDK.CadObjects;
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
using CadProjectorSDK.CadObjects.LObjects;

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
        public async static Task<PointsObjectList> GetLObject (ProjectionScene scene)
        {
            Processing = true;
            PointsObjectList dotList = new PointsObjectList();

            foreach (object obj in scene.Objects)
            {
                if (obj is CanvasObject cadObject && cadObject.Render == true)
                {
                    dotList.AddRange(SceneSender.GetPoint(cadObject, false));
                    //dotList.AddRange(cadObject.GetTransformPoint(false));
                }
            }
            PointsObjectList outList = new PointsObjectList();
            if (scene.Masks.Count > 0)
            {
                foreach (CanvasRectangle rectangle in scene.Masks)
                {
                   outList.AddRange(await rectangle.LRect.GetCutObjects(dotList));
                }
            }
            else outList = dotList;

            Processing = false;

            if (true)
            {
                outList.Add(new PointsObject() { new CadPoint3D(scene.MousePosition.MX, scene.MousePosition.MY) });
            }

            return outList;
        }


        /// <summary>
        /// Get point from inner object
        /// </summary>
        /// <param name="cadObject">inner object</param>
        /// <returns>Object collection</returns>
        public static PointsObjectList GetPoint(CanvasObject cadObject, bool InGroup)
        {
            PointsObjectList lObjectList = new PointsObjectList();

            switch (cadObject)
            {
                case CanvasAnchor cadDot:
                    if (cadObject.DataContext is LDeviceMesh deviceMesh)
                    {
                        if (LDeviceMesh.ClbrForm == CalibrationForm.cl_Dot) 
                            lObjectList.Add(new PointsObject() { Points = new List<CadPoint3D>() { cadDot.GetLPoint.GetMLpoint3D }, MeshType = cadObject.MeshType });
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
                        lObjectList.Add(new PointsObject()
                        {
                            Points = new List<CadPoint3D>()
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
                case CanvasLine cadLine:
                    lObjectList.Add(new PointsObject()
                    {
                        Points = new List<CadPoint3D>() {
                        cadLine.P1,
                        cadLine.P2
                        },
                        ProjectionSetting = cadLine.ProjectionSetting
                    });
                    break;
                case CanvasRectangle cadRectangle:
                    lObjectList.Add(new PointsObject()
                    {
                        Points = new List<CadPoint3D>() {
                        cadRectangle.LRect.P1,
                        new CadPoint3D(cadRectangle.LRect.P2.MX, cadRectangle.LRect.P1.MY),
                        cadRectangle.LRect.P2,
                        new CadPoint3D(cadRectangle.LRect.P1.MX, cadRectangle.LRect.P2.MY),
                        },
                        ProjectionSetting = cadRectangle.ProjectionSetting,
                        IsClosed = true
                    });
                    break;
                case CadObjectsGroup cadObjectsGroup:
                    foreach (CanvasObject obj in cadObjectsGroup)
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
            PointsObjectList CalibrationCross(LDeviceMesh monchaDeviceMesh, CadPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;


                //Vertical
                PointsObject Line1 = new PointsObject();

                for (int i = 0; i <= width; i += 1)
                {
                    Line1.Add(monchaDeviceMesh[tuple.Item2, i].GetMLpoint3D);
                }

                //Vertical

                //Horizontal
                PointsObject Line2 = new PointsObject();

                for (int i = 0; i <= height; i += 1)
                {
                    Line2.Add(monchaDeviceMesh[i, tuple.Item1].GetMLpoint3D);
                }

                return new PointsObjectList()
            {
                Line2,
                Line1

            };

            }
            PointsObjectList CalibrationRect(LDeviceMesh monchaDeviceMesh, CadPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;

                //Vertical
                PointsObject Line1H = GetLine(tuple.Item1, tuple.Item2, true);
                //Horizontal
                PointsObject Line1W = GetLine(tuple.Item1, height - tuple.Item2, false);
                //Vertical
                PointsObject Line2H = GetLine(width - tuple.Item1, height - tuple.Item2, true);
                //Horizontal
                PointsObject Line2W = GetLine(width - tuple.Item1, tuple.Item2, false);


                return new PointsObjectList()
                {
                    Line1H,
                    Line1W,
                    Line2H,
                    Line2W
                };


                PointsObject GetLine(int xpos, int ypos, bool Vertical)
                {
                    PointsObject Line = new PointsObject() { MeshType = cadObject.MeshType };

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
            PointsObjectList CalibrationMiniRect(LDeviceMesh monchaDeviceMesh, CadPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;

                return new PointsObjectList()
                {
                        new PointsObject(){
                            Points = new List<CadPoint3D>(){
                                monchaDeviceMesh[tuple.Item2, tuple.Item1].GetMLpoint3D,
                                monchaDeviceMesh[tuple.Item2, tuple.Item1 + (tuple.Item1 < width ? 1 : -1)].GetMLpoint3D,
                                monchaDeviceMesh[tuple.Item2 + (tuple.Item2 < height ? 1 : -1), tuple.Item1 + (tuple.Item1 < width ? 1 : -1)].GetMLpoint3D,
                                monchaDeviceMesh[tuple.Item2 + (tuple.Item2 < height ? 1 : -1), tuple.Item1].GetMLpoint3D
                            },
                            IsClosed = true,
                            MeshType = cadObject.MeshType
                        }
                };

            }
            PointsObjectList CalibrationLineH(LDeviceMesh monchaDeviceMesh, CadPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;


                //Height
                PointsObject Line = new PointsObject() { MeshType = cadObject.MeshType };

                for (int i = 0; i <= height; i += 1)
                {
                    Line.Add(monchaDeviceMesh[i, tuple.Item1].GetMLpoint3D);
                }

                return new PointsObjectList()
                {
                    Line,
                };

            }
            PointsObjectList CalibrationLineW(LDeviceMesh monchaDeviceMesh, CadPoint3D lPoint3D)
            {
                Tuple<int, int> tuple = monchaDeviceMesh.CoordinatesOf(lPoint3D);
                int height = monchaDeviceMesh.GetLength(0) - 1;
                int width = monchaDeviceMesh.GetLength(1) - 1;

                //width
                PointsObject Line = new PointsObject() { MeshType = cadObject.MeshType }; ;

                for (int i = 0; i <= width; i += 1)
                {
                    Line.Add(monchaDeviceMesh[tuple.Item2, i].GetMLpoint3D);
                }

                return new PointsObjectList()
                {
                    Line
                };

            }
            #endregion
        }

        /// <summary>
        /// Convert inner object in LPoint3D's
        /// </summary>
        public async static  Task<PointsObjectList> CalcContour(CanvasObject cadObject)
        {
            PointsObjectList PathList = new PointsObjectList();

            switch ((object)cadObject)
            {
                case NurbsShape nurbsShape:
                    PointsObject NurbsObject = new PointsObject();
                    foreach (Point nurbsPoint in nurbsShape.BSplinePoints(cadObject.ProjectionSetting.PointStep.MX))
                    {
                        NurbsObject.Add(new CadPoint3D(nurbsPoint));
                    }
                    NurbsObject.ProjectionSetting = cadObject.ProjectionSetting;
                    PathList.Add(NurbsObject);
                    break;

                case CadGeometry cadContour:

                    foreach(PointsElement pntobj in cadContour.GCObject.GetPointCollection(cadContour.TransformGroup, cadObject.ProjectionSetting.PointStep.MX, cadObject.ProjectionSetting.RadiusEdge))
                    {
                        PathList.Add(new PointsObject(pntobj.GetPoints3D));
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
        public static PointsObject QBezierByStep(Point StartPoint, Point ControlPoint, Point EndPoint, double CRS)
        {
            CadPoint3D LastPoint = new CadPoint3D(StartPoint);
            double Lenth = 0;
            for (int t = 1; t < 100; t++)
            {
                CadPoint3D tempPoint = GetPoint((double)t / 99);
                Lenth += CadPoint3D.Lenth2D(LastPoint, tempPoint);
                LastPoint = tempPoint;
            }

            int CountStep = (int)(Lenth / (CRS)) >= 2 ? (int)(Lenth / CRS) : 2;

            PointsObject tempObj = new PointsObject();

            for (int t = 0; t < CountStep; t++)
            {
                tempObj.Add(GetPoint((double)t / (CountStep - 1)));
            }

            return tempObj;

            CadPoint3D GetPoint(double t)
            {
                return new CadPoint3D(
                    (1 - t) * (1 - t) * StartPoint.X + 2 * (1 - t) * t * ControlPoint.X + t * t * EndPoint.X,
                   (1 - t) * (1 - t) * StartPoint.Y + 2 * (1 - t) * t * ControlPoint.Y + t * t * EndPoint.Y);
            }
        }

        /// <summary>
        /// interpolation bezier
        /// </summary>
        public static PointsObject BezieByStep(Point point0, Point point1, Point point2, Point point3, double CRS)
        {
            double Lenth = 0;
            CadPoint3D LastPoint = new CadPoint3D(point1);

            for (int t = 0; t < 100; t++)
            {
                CadPoint3D tempPoint = GetPoint((double)t / 99);
                Lenth += CadPoint3D.Lenth2D(LastPoint, tempPoint);
                LastPoint = tempPoint;
            }

            PointsObject tempObj = new PointsObject();

            int CountStep = (int)(Lenth / CRS) >= 2 ? (int)(Lenth / CRS) : 2;

            for (int t = 0; t < CountStep; t++)
            {
                tempObj.Add(GetPoint((double)t / (CountStep - 1)));
            }

            return tempObj;

            CadPoint3D GetPoint(double t)
            {
                return new CadPoint3D(
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
