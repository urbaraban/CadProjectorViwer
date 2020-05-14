using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace MonchaCadViewer.CanvasObj
{
    public static class SendProcessor
    {
        public static void Worker (Viewbox CanvasBox, bool calibrationstat)
        {
            if (CanvasBox.Child is CadCanvas canvas)
            {
                LObjectList tempList = new LObjectList();
                foreach (CadObject cadObject in canvas.Children)
                {
                    if (cadObject.Render)
                    {
                        if (cadObject is ViewContour polygon)
                        {
                            double left = Canvas.GetLeft(polygon);
                            double top = Canvas.GetTop(polygon);

                            foreach (List<MonchaPoint3D> points in polygon.GiveModPoint())
                            {
                                LObject lContour = new LObject();
                                foreach (MonchaPoint3D point3D in points)
                                    lContour.Add(new MonchaPoint3D(left + point3D.X, top + point3D.Y, point3D.Z, point3D.M));
                                lContour.Closed = true;
                                tempList.Add(lContour);

                            }
                        }

                        if (cadObject is LineSbcr line)
                        {

                            LObject lContour = new LObject();

                            lContour.Add((MonchaPoint3D)line.BaseContextPoint);
                            lContour.Add((MonchaPoint3D)line.SecondContextPoint);
                        
                            if (lContour.Count > 1)
                                tempList.Add(lContour);
                        }

                        if (cadObject is DotShape dpoint)
                        {
                            if (dpoint.IsSelected)
                            {
                                LObject lObject = new LObject(
                                    new List<MonchaPoint3D>() {
                                        new MonchaPoint3D(
                                            dpoint.MultPoint.X,
                                            dpoint.MultPoint.Y, //inverted
                                            dpoint.MultPoint.Z) });

                                tempList.Add(lObject);
                            }
                        }
                    }


                    tempList.OnBaseMesh = cadObject.OnBaseMesh;

                }

                tempList.Bop = new Point3D(0, 0, 0);
                tempList.Top = new Point3D(canvas.ActualWidth, canvas.ActualHeight, canvas.ActualWidth);


                if (tempList.Count > 0)
                {
                    MonchaHub.MainFrame = tempList;
                    MonchaHub.RefreshFrame();
                }

            }


        }
        public static void DrawZone(MonchaDevice device)
        {
            LObjectList tempList = new LObjectList();

            tempList.Bop = new Point3D(0, 0, 0);
            tempList.Top = new Point3D(1, 1, 1);

            LObject lObject = new LObject();
            lObject.Add(new MonchaPoint3D(0, 0, 0, 1));
            lObject.Add(new MonchaPoint3D(0, device.Size.Y, 0, 1));
            lObject.Add(new MonchaPoint3D(device.Size.X, device.Size.Y, 0, 1));
            lObject.Add(new MonchaPoint3D(device.Size.X, 0, 0, 1));
            lObject.Closed = true;

            tempList.Add(lObject);

            tempList.OnBaseMesh = false;

            if (tempList.Count > 0)
            {
                MonchaHub.MainFrame = tempList;
                MonchaHub.RefreshFrame();
            }
        }


    }
}
