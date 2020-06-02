using Kompas6API5;

using Kompas6Constants;
using KompasLib.Tools;
using reference = System.Int32;
using Kompas6API7;
using Point3D = System.Windows.Media.Media3D.Point3D;
using System.Windows;
using MonchaSDK.Object;
using KAPITypes;
using MonchaSDK;
using System;

namespace MonchaCadViewer.Format
{
    public static class KMPS
    {
        public static LObjectList GetContour(double crs, double mash, bool add, bool cursor = true)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                #region получение контура
                double x = 0, y = 0;
                RequestInfo info = (RequestInfo)KmpsAppl.KompasAPI.GetParamStruct((short)StructType2DEnum.ko_RequestInfo);

                //Ищем или находим макрообъект по индексу потолка
                IMacroObject macroObject = KmpsAppl.Doc.Macro.FindCeilingMacro("0");
                //Создаем если нет
                if (macroObject == null) macroObject = KmpsAppl.Doc.Macro.MakeCeilingMacro("0");

                if (cursor)
                {
                    if (!add)
                    {
                        KmpsAppl.Doc.Macro.RemoveCeilingMacro("0");
                        macroObject = KmpsAppl.Doc.Macro.MakeCeilingMacro("0");
                    }
                    int j = KmpsAppl.Doc.D5.ksCursor(info, ref x, ref y, 0);
                    if (!KmpsAppl.Doc.Macro.AddCeilingMacro(KmpsAppl.Doc.D5.ksMakeEncloseContours(0, x, y), "0")) MessageBox.Show("Контур не добавили", "Ошибка"); //Добавляем ksMakeEncloseContours
                }

                ksInertiaParam inParam = (ksInertiaParam)KmpsAppl.KompasAPI.GetParamStruct((short)StructType2DEnum.ko_InertiaParam);
                ksIterator Iterator1 = (ksIterator)KmpsAppl.KompasAPI.GetIterator();
                Iterator1.ksCreateIterator(ldefin2d.ALL_OBJ, macroObject.Reference);


                reference refContour1 = Iterator1.ksMoveIterator("F");
                ksRectParam spcGabarit = (ksRectParam)KmpsAppl.KompasAPI.GetParamStruct((short)StructType2DEnum.ko_RectParam);
                
                
                LObjectList contoursList = new LObjectList();

                contoursList.DisplayName = KmpsAppl.Doc.D7.Name;

                //
                //Начинаем перебор контуров со всем что есть
                //
                //Заходим в первый контур
                refContour1 = Iterator1.ksMoveIterator("F");

                while (refContour1 != 0)
                {
                    IContour contour = KmpsAppl.Doc.Macro.GiveContour(refContour1);

                    if (contour != null)
                    {
                        LObject contour1 = TraceContour(contour);
                        contoursList.Add(contour1);
                    }

                    refContour1 = Iterator1.ksMoveIterator("N"); //Двигаем итератор 1
                }

                Iterator1.ksDeleteIterator(); //Удаляем итератор 1 после полного перебора

                return ReadyFrame.PointsMagic(contoursList, true);

                #endregion
            }
            else MessageBox.Show("Объект не захвачен", "Сообщение");

            return null;

            LObject TraceContour(IContour contour)
            {
                if (contour != null)
                {
                    LObject lContour = new LObject();

                    for (int i = 0; i < contour.Count; i++)
                    {
                        IContourSegment pDrawObj = (IContourSegment)contour.Segment[i];
                        // Получить тип объекта
                        try
                        {
                            //Если прямая
                            IContourLineSegment contourLineSegment = (IContourLineSegment)pDrawObj;

                            //Получаем с нее точки
                            MonchaPoint3D point1 = new MonchaPoint3D(contourLineSegment.X1, contourLineSegment.Y1, 0, 1);
                            MonchaPoint3D point2 = new MonchaPoint3D(contourLineSegment.X2, contourLineSegment.Y2, 0, 1);

                            if (lContour.IndexOf(point1) == -1) //Если координата уже занесена, то нахуй
                                lContour.Add(point1);
                            if (lContour.IndexOf(point2) == -1) //Если координата уже занесена, то нахуй
                                lContour.Add(point2);
                        }
                        catch
                        {
                            ICurve2D contourLineSegment = (ICurve2D)pDrawObj.Curve2D;
                            double[] arrayCurve = contourLineSegment.CalculatePolygonByStep(crs / contourLineSegment.Length);
                            for (int j = 0; j < arrayCurve.Length; j += 2)
                                if (lContour.IndexOf(new MonchaPoint3D(arrayCurve[j], arrayCurve[j + 1], 0)) == -1) //Если координата занесена
                                {
                                    lContour.Add(new MonchaPoint3D(arrayCurve[j], arrayCurve[j + 1], 0, (byte)(j == 0 || j == arrayCurve.Length - 2 ? 1 : 0)));
                                }
                        }
                    }
                    lContour.Closed = true;
                    return lContour;
                }
                return null;
            }
        }


      

    }
}
