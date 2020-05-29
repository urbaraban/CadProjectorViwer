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
        public static void Worker(CadCanvas canvas)
        {
            LObjectList tempList = new LObjectList();
            foreach (CadObject cadObject in canvas.Children)
            {
                if (cadObject.Render)
                {
                    if (cadObject is CadContour polygon)
                    {
                        foreach (List<MonchaPoint3D> points in polygon.GiveModPoint())
                        {
                            LObject lContour = new LObject();
                            foreach (MonchaPoint3D point3D in points)
                                lContour.Add(new MonchaPoint3D(polygon.BaseContextPoint.GetMPoint.X + point3D.X,
                                    polygon.BaseContextPoint.GetMPoint.Y + point3D.Y,
                                    point3D.Z, point3D.T));
                            lContour.Closed = true;
                            tempList.Add(lContour);

                        }
                    }

                    if (cadObject is CadLine line)
                    {

                        LObject lContour = new LObject();

                        lContour.Add(line.BaseContextPoint.GetMPoint3D);
                        lContour.Add(line.SecondContextPoint.GetMPoint3D);

                        if (lContour.Count > 1)
                            tempList.Add(lContour);
                    }

                    if (cadObject is CadDot dpoint)
                    {
                        if (dpoint.IsSelected)
                        {
                            LObject lObject = new LObject(
                                new List<MonchaPoint3D>() {
                                        new MonchaPoint3D(
                                            dpoint.BaseContextPoint.GetMPoint.X,
                                            dpoint.BaseContextPoint.GetMPoint.Y)
                                });

                            tempList.Add(lObject);
                        }
                    }

                    if (cadObject is CadRectangle cadRectangle)
                    {
                        LObject lObject = new LObject(
                            new List<MonchaPoint3D>() {
                                        new MonchaPoint3D(
                                            cadRectangle.BaseContextPoint.GetMPoint.X,
                                            cadRectangle.BaseContextPoint.GetMPoint.Y),
                                        new MonchaPoint3D(
                                            cadRectangle.BaseContextPoint.GetMPoint.X,
                                            cadRectangle.SecondContextPoint.GetMPoint.Y),
                                         new MonchaPoint3D(
                                            cadRectangle.SecondContextPoint.GetMPoint.X,
                                            cadRectangle.SecondContextPoint.GetMPoint.Y),
                                         new MonchaPoint3D(
                                            cadRectangle.SecondContextPoint.GetMPoint.X,
                                            cadRectangle.BaseContextPoint.GetMPoint.Y),

                            });

                        tempList.Add(lObject);

                    }
                }


                tempList.OnBaseMesh = cadObject.OnBaseMesh;

            }

            tempList.Bop = new MonchaPoint3D(0, 0, 0);
            tempList.Top = MonchaHub.Size.GetMPoint3D;


            if (tempList.Count > 0)
            {
                MonchaHub.MainFrame = tempList;
                MonchaHub.RefreshFrame();
            }
        }

        public static void DrawZone(MonchaDevice device)
        {
            LObjectList tempList = new LObjectList();

            tempList.Bop = new MonchaPoint3D(0, 0, 0);
            tempList.Top = new MonchaPoint3D(1, 1, 1);

            LObject lObject = new LObject();
            lObject.Add(new MonchaPoint3D(device.TBOP.X * MonchaHub.Size.X, device.TBOP.Y * MonchaHub.Size.Y, 0, 1));
            lObject.Add(new MonchaPoint3D(device.TTOP.X * MonchaHub.Size.X, device.TBOP.Y * MonchaHub.Size.Y, 0, 1));
            lObject.Add(new MonchaPoint3D(device.TTOP.X * MonchaHub.Size.X, device.TTOP.Y * MonchaHub.Size.Y, 0, 1));
            lObject.Add(new MonchaPoint3D(device.TBOP.X * MonchaHub.Size.X, device.TTOP.Y * MonchaHub.Size.Y, 0, 1));
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
