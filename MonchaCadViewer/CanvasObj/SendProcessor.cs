using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using AppSt = MonchaCadViewer.Properties.Settings;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System;
using System.IO;

namespace MonchaCadViewer.CanvasObj
{
    public static class SendProcessor
    {
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

            PathGeometry pathGeometry = cadObject.GmtrObj as PathGeometry;
            
            foreach (PathFigure figure in pathGeometry.Figures)
            {
                LObject lObject = new LObject();
                lObject.Closed =  figure.IsClosed;

                lObject.Add(new LPoint3D(figure.StartPoint));

                Point LastPoint = figure.StartPoint;
                foreach (PathSegment segment in figure.Segments)
                {
                    
                    switch (segment.GetType().FullName)
                    {
                        case "System.Windows.Media.BezierSegment":
                            System.Windows.Media.BezierSegment bezierSegment = (System.Windows.Media.BezierSegment)segment;
                            lObject.AddRange(BezieByStep(LastPoint, bezierSegment, AppSt.Default.cl_crs));
                            LastPoint = bezierSegment.Point3;
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
                        case "System.Windows.Media.QuadraticBezierSegment":
                            System.Windows.Media.QuadraticBezierSegment quadraticBezierSegment = (System.Windows.Media.QuadraticBezierSegment)segment;
                            break;
                        case "System.Windows.Media.ArcSegment":
                            System.Windows.Media.ArcSegment arcSegment = (System.Windows.Media.ArcSegment)segment;
                            break;
                    }
                }

                if (lObject.Count > 0)
                    lObjectList.Add(PointsMagic(lObject, cadObject));
            }
            return lObjectList;
        }

        public static LObject BezieByStep(Point pointstart, BezierSegment bezier, double step)
        {
            double Lenth = 0;
            LPoint3D LastPoint = new LPoint3D(pointstart);

            for (int t = 0; t < 100; t++)
            {
                LPoint3D tempPoint = GetPoint((double)t / 99);
                Lenth += LPoint3D.Lenth(LastPoint, tempPoint);
                LastPoint = tempPoint;
            }

            LObject tempObj = new LObject();

            int CountStep = (int)(Lenth / step);

            for (int t = 0; t < CountStep; t++)
            {
                tempObj.Add(GetPoint((double)t / (CountStep - 1)));
            }

            return tempObj;

            LPoint3D GetPoint(double t)
            {
                return new LPoint3D(
                    ((1 - t) * (1 - t) * (1 - t)) * pointstart.X
                           + 3 * ((1 - t) * (1 - t)) * t * bezier.Point1.X
                           + 3 * (1 - t) * (t * t) * bezier.Point2.X
                           + (t * t * t) * bezier.Point3.X,
                    ((1 - t) * (1 - t) * (1 - t)) * pointstart.Y
                       + 3 * ((1 - t) * (1 - t)) * t * bezier.Point1.Y
                       + 3 * (1 - t) * (t * t) * bezier.Point2.Y
                       + (t * t * t) * bezier.Point3.Y, (byte)1);
            }
        }

        public static LObject PointsMagic(LObject lObject, CadObject baseobj)
        {
            if (lObject.Count > 0)
            {
                //зеркалим
                if (baseobj.Mirror)
                {
                    //не влияет на габарит, так как вокруг центра Х
                    pointMirror(lObject);
                }

                //Вращаем
                if (baseobj.Rotate.Angle != 0)
                {
                    
                   pointRotate(lObject);
                }

                //смещаем
                pointOffcet(lObject);
            }

            return lObject;

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
                    double angleInRadians = baseobj.Rotate.Angle * ((float)Math.PI / 180);
                    double cosTheta = Math.Cos(angleInRadians);
                    double sinTheta = Math.Sin(angleInRadians);
                    pointToRotate.Update(
                            // X
                            (cosTheta * (pointToRotate.X - baseobj.Rotate.CenterX) -
                            sinTheta * (pointToRotate.Y - baseobj.Rotate.CenterY) + baseobj.Rotate.CenterX),
                            //Y
                            (sinTheta * (pointToRotate.X - baseobj.Rotate.CenterX) +
                            cosTheta * (pointToRotate.Y - baseobj.Rotate.CenterY) + baseobj.Rotate.CenterY),
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
                    _obj[i].X += baseobj.Translate.X;
                    _obj[i].Y += baseobj.Translate.Y;
                }
                return _obj;
            }

            //Функция отзеркаливания
            LObject pointMirror(LObject _obj)
            {
                for (int i = 0; i < _obj.Count; i++)
                    _obj[i].X = 0 - _obj[i].X;
                return _obj;
            }
        }


    }
}
