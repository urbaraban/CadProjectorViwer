using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using MonchaSDK;
using MonchaSDK.Object;
using System;
using System.IO;

namespace MonchaCadViewer.Format
{
    public static class DXF
    {
        public static LObjectList Get(string filename)
        {
            DxfFile dxfFile;
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                dxfFile = DxfFile.Load(fs);
            }

            if (dxfFile != null)
            {
                LObjectList contoursList = new LObjectList();
                LObject contour = new LObject();

                foreach (DxfEntity entity in dxfFile.Entities)
                {
                    switch (entity.EntityType)
                    {
                        case DxfEntityType.Line:
                            DxfLine line = (DxfLine)entity;

                            //Если у нас уже есть точки, а новая точка не входит в массив, то это ж-ж-ж не с проста и у нас новая фигура
                            if (contour.Count > 1 && contour.IndexOf(new MonchaPoint3D(line.P1.X, line.P1.Y, line.P1.Z, 1)) == -1)
                            {
                                contoursList.Add(contour);
                                contour = new LObject();
                            }

                            MonchaPoint3D point1 = new MonchaPoint3D(line.P1.X, line.P1.Y, line.P1.Z, 1);
                            if (contour.IndexOf(point1) == -1)
                                contour.Add(point1);

                            MonchaPoint3D point2 = new MonchaPoint3D(line.P2.X, line.P2.Y, line.P2.Z, 1);
                            if (contour.IndexOf(point2) == -1)
                                contour.Add(point2);

                            if (contour.Count > 0 && point2 == contour.First())
                                contour.Closed = true; //замыкаем 
                            //Получаем с нее точки

                            break;
                        case DxfEntityType.MLine:
                            break;
                        case DxfEntityType.Arc:
                            break;
                        case DxfEntityType.Circle:
                            break;
                        case DxfEntityType.Ellipse:
                            break;
                        case DxfEntityType.LwPolyline:
                            DxfLwPolyline dxfLwPolyline = (DxfLwPolyline)entity;
                            if (contour.Count > 2)
                            {
                                contoursList.Add(contour);
                                contour = new LObject();
                            }
                            //Идем по точкам
                            for (int i = 0; i < dxfLwPolyline.Vertices.Count; i++)
                                contour.Add(new MonchaPoint3D(dxfLwPolyline.Vertices[i % dxfLwPolyline.Vertices.Count].X, dxfLwPolyline.Vertices[i % dxfLwPolyline.Vertices.Count].Y, 0, 1));
                            contour.Closed = true;

                            break;
                        case DxfEntityType.Polyline:
                            DxfPolyline dxfPolyline = (DxfPolyline)entity;
                            if (contour.Count > 2)
                            {
                                contoursList.Add(contour);
                                contour = new LObject();
                            }
                            for (int i = 0; i < dxfPolyline.Vertices.Count; i++)
                                contour.Add(new MonchaPoint3D((float)dxfPolyline.Vertices[i].Location.X, (float)dxfPolyline.Vertices[i].Location.Y, dxfPolyline.Vertices[i].Location.Z, 1));
                            contour.Closed = true;
                            break;
                    }
                }
                contoursList.Add(contour);
                contoursList.DisplayName = "DXF";
                return ReadyFrame.PointsMagic(contoursList);
            }

            return null;
        }
    }
}
