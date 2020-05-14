using IxMilia.Stl;
using MonchaSDK;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.IO;

namespace MonchaCadViewer.Format
{
    public static class STL
    {
        public static LObjectList Get(string filepath)
        {
            StlFile stlFile;
            if (File.Exists(filepath))
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open))
                {
                    stlFile = StlFile.Load(fs);
                }

                if (stlFile != null)
                {
                    //Лист сегментов
                    LObjectList SegmentList = new LObjectList();
                    //лист полигонов в фигуре
                    List<StlTriangle> triangles = stlFile.Triangles;

                    //Ищем грани в 90 градусов
                    for (int j = 0; j < triangles.Count - 1; j++)
                        for (int k = j + 1; k < triangles.Count; k++)
                        {
                            LObject MegeVertex = findPolygonAngle(triangles[j], triangles[k]);
                            if (MegeVertex != null)
                                SegmentList.Add(MegeVertex);//добавляем в виде лайнсегментов
                        }


                    LObjectList Figures = new LObjectList();
                    //Добавляем точки в коллекцию для дальнейшей обработки

                    //Составляем контуры
                    for (int i = 0; i < SegmentList.Count; i++)
                    {
                        LObjectList tempList = new LObjectList();
                        tempList.Add(SegmentList[i]);
                        SegmentList.RemoveAt(i);
                        for (int j = 0; j < SegmentList.Count; j++)
                        {
                            //Если совпадает последняя координата набор с первой координатой сегмента

                            if (tempList.Last().Last() == SegmentList[j].First())
                            {
                                if (CheckOnPlace(tempList.Last().First(), tempList.Last().Last(), SegmentList[j].First(), SegmentList[j].Last()))
                                {
                                    tempList.Add(SegmentList[j]);
                                    SegmentList.RemoveAt(j);
                                    j--;
                                }
                            }
                            //Если совпадает первая координата набора с первой координатой сегмента
                            else if (tempList.First().First() == SegmentList[j].First())
                            {
                                if (CheckOnPlace(tempList.First().First(), tempList.First().Last(), SegmentList[j].First(), SegmentList[j].Last()))
                                {
                                    tempList.Insert(0, new LObject() { SegmentList[j].Last(), SegmentList[j].First() });
                                    SegmentList.RemoveAt(j);
                                    j--;
                                }
                            }
                            //Если совпадает первая координата набор с последней координатой сегмента
                            else if (tempList.First().First() == SegmentList[j].Last())
                            {
                                if (CheckOnPlace(tempList.First().First(), tempList.First().Last(), SegmentList[j].Last(), SegmentList[j].First()))
                                {
                                    tempList.Insert(0, SegmentList[j]);
                                    SegmentList.RemoveAt(j);
                                    j--;
                                }
                            }
                            //Если совпадает последняя координата набора с последней координатой сегмента
                            else if (tempList.Last().Last() == SegmentList[j].Last())
                            {
                                if (CheckOnPlace(tempList.Last().First(), tempList.Last().Last(), SegmentList[j].Last(), SegmentList[j].First()))
                                {
                                    tempList.Add(new LObject() { SegmentList[j].Last(), SegmentList[j].First() });
                                    SegmentList.RemoveAt(j);
                                    j--;
                                }
                            }

                        }
                        if (tempList.Count > 0)
                            Figures.Add(ConvertSegmet(tempList));
                    }
                    Figures.DisplayName = "STL";
                    return ReadyFrame.PointsMagic(Figures);
                }
                return null;
            }

            return null;

            LObject ConvertSegmet(LObjectList inList)
            {
                LObject temp = new LObject();

                for (int i = 0; i < inList.Count; i++)
                {
                    temp.Add(inList[i][0]);
                    if (inList[i][1] == temp.First())
                        temp.Closed = true;
                }

                return temp;
            }

            bool CheckAxis(MonchaPoint3D stlVertex21, MonchaPoint3D stlVertex2)
            {
                if (stlVertex21.X == stlVertex2.X || stlVertex21.Y == stlVertex2.Y || stlVertex21.Z == stlVertex2.Z)
                    return true;
                return false;
            }

            bool CheckOnPlace(MonchaPoint3D point_1, MonchaPoint3D point_2, MonchaPoint3D point_3, MonchaPoint3D FindPoint)
            {
                double A = point_1.Y * (point_2.Z - point_3.Z) + point_2.Y * (point_3.Z - point_1.Z) + point_3.Y * (point_1.Z - point_2.Z);
                double B = point_1.Z * (point_2.X - point_3.X) + point_2.Z * (point_3.X - point_1.X) + point_3.Z * (point_1.X - point_2.X);
                double C = point_1.X * (point_2.Y - point_3.Y) + point_2.X * (point_3.Y - point_1.Y) + point_3.X * (point_1.Y - point_2.Y);
                double D = point_1.X * (point_2.Y * point_3.Z - point_3.Y * point_2.Z) + point_2.X * (point_3.Y * point_1.Z - point_1.Y * point_3.Z) + point_3.X * (point_1.Y * point_2.Z - point_2.Y * point_1.Z);

                if (A * FindPoint.X + B * FindPoint.Y + C * FindPoint.Z + D == 0)
                    return true;

                return false;
            }

            //Проверяет есть ли между полигонами связь (две вершины) и есть ли между ними нужный угол (+-90)
            LObject findPolygonAngle(StlTriangle trinagle1, StlTriangle trinagle2)
            {
                double degresDelta = 0.3;

                LObject mergePoint = new LObject(); //счетчик 
                                                      //Проверяем совпадение вершин
                foreach (StlVertex stlVertex1 in trinagle1.Vertexs)
                    foreach (StlVertex stlVertex2 in trinagle2.Vertexs)
                        if (stlVertex1.X == stlVertex2.X && stlVertex1.Y == stlVertex2.Y && stlVertex1.Z == stlVertex2.Z)
                            mergePoint.Add(new MonchaPoint3D(stlVertex1.X, stlVertex1.Y, stlVertex1.Z));

                //Если у нас совпадают две точки, то можем измерить угол
                if (mergePoint.Count == 2)
                {
                    double cosa = Math.Abs(trinagle1.Normal.X * trinagle2.Normal.X + trinagle1.Normal.Y * trinagle2.Normal.Y + trinagle1.Normal.Z * trinagle2.Normal.Z) /
                    (Math.Sqrt(Math.Pow(trinagle1.Normal.X, 2) + Math.Pow(trinagle1.Normal.Y, 2) + Math.Pow(trinagle1.Normal.Z, 2)) *
                     Math.Sqrt(Math.Pow(trinagle2.Normal.X, 2) + Math.Pow(trinagle2.Normal.Y, 2) + Math.Pow(trinagle2.Normal.Z, 2)));
                    if (cosa > -degresDelta && cosa < degresDelta)
                        return mergePoint; //Возвращаем точки грани
                }
                return null;
            }
        }
    }
}
