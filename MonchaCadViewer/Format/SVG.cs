using MonchaSDK;
using MonchaSDK.Object;
using System.Linq;
using System.Xml.Linq;

namespace MonchaCadViewer.Format
{
    public static class SVG
    {
        public static LObjectList Get(string filename, char separator1, char separator2)
        {
            XDocument xdoc = XDocument.Load(filename);
            if (xdoc != null)
            {
                XNamespace xNamespace = xdoc.Root.GetDefaultNamespace();
                XElement xElement = xdoc.Root;
                LObjectList contoursList = new LObjectList();
                //Каждая группа — новый лист
                if (xElement.Descendants(xElement.Name.Namespace + "g").Count() > 0)
                    for (int Group = 0; Group < xElement.Descendants(xElement.Name.Namespace + "g").Count(); Group++)
                        for (int Contour = 0; Contour < xElement.Descendants(xElement.Name.Namespace + "g").ElementAt(Group).Descendants(xElement.Name.Namespace + "polygon").Count(); Contour++)
                        {
                            XElement polygon = xElement.Descendants(xElement.Name.Namespace + "g").ElementAt(Group).Descendants(xElement.Name.Namespace + "polygon").ElementAt(Contour);
                            //Получаем поинты из контура
                            LObject contour = GivePoints(polygon.Attribute("points").Value);
                            contoursList.Add(contour);
                        }
                else
                {
                    for (int Contour = 0; Contour < xElement.Descendants(xElement.Name.Namespace + "polygon").Count(); Contour++)
                    {
                        XElement polygon = xElement.Descendants(xElement.Name.Namespace + "polygon").ElementAt(Contour);
                        //Получаем поинты из контура
                        LObject contour = GivePoints(polygon.Attribute("points").Value);
                        contoursList.Add(contour);
                    }
                }
                contoursList.DisplayName = "SVG";
                return ReadyFrame.PointsMagic(contoursList);
            }

            return null;

            LObject GivePoints(string stroke)
            {
                LObject tempContour = new LObject();

                string[] pointPolygon = stroke.Split(separator1 == '\0' ? ' ' : separator1);
                if (pointPolygon != null)
                {
                    if (separator1 == separator2)
                    {
                        for (int j = 0; j < pointPolygon.Length - 2; j += 2)
                        {
                            try
                            {
                                tempContour.Add(new MonchaPoint3D(double.Parse(pointPolygon[j].Replace('.', ',')), double.Parse(pointPolygon[j + 1].Replace('.', ',')), 0, 1));
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0; j < pointPolygon.Length; j++)
                        {
                            separator2 = char.IsWhiteSpace(separator2) ? ' ' : separator2;

                            if (pointPolygon[j].Length > 0)
                                tempContour.Add(new MonchaPoint3D(double.Parse(pointPolygon[j].Split(separator2)[0].Replace('.', ',')), double.Parse(pointPolygon[j].Split(separator2)[1].Replace('.', ',')), 0, 1));
                        }
                    }
                }

                tempContour.Closed = true;
                return tempContour;
            }
        }
    }
}
