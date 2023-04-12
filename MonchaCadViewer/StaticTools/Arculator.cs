using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ToGeometryConverter;

namespace CadProjectorViewer.StaticTools
{
    internal class Arculator
    {
        public static async Task<object> Parse(string path)
        {
            FileInfo fileInfo= new FileInfo(path);
            if (fileInfo.Exists == true)
            {
                CadGroup result = new CadGroup(fileInfo.Name);
                string[] file = File.ReadAllLines(path);
                for (int i = 0; i < file.Length; i += 1)
                {
                    string line = file[i];
                    if (line == "ROOMBEGIN")
                    {
                        int end_index = i;
                        while (end_index < file.Length && file[end_index] != "ROOMEND")
                            end_index += 1;

                        if (end_index < file.Length && file[end_index] == "ROOMEND")
                        {
                            result.Add(ParseRoom(file.Skip(i + 1).Take(end_index - i).ToArray()));
                        }
                    } 
                }
                return result;
            }
            return null;
        }

        public static UidObject ParseRoom(string[] room_str)
        {
            if (room_str.Length > 0)
            {
                CadGroup room = new CadGroup(room_str[0].Split(' ')[1]);
                PathGeometry pathGeometry = new PathGeometry();
                PointCollection points = new PointCollection();
                double scaleX = 1;
                double scaleY = 1;
                for (int i = 0; i < room_str.Length; i += 1)
                {
                    string line = room_str[i];
                    if (line == "NPLine")
                    {
                        points.Add(ParsePoint(room_str[i + 1]));
                        points.Add(ParsePoint(room_str[i + 3]));
                        points.Add(ParsePoint(room_str[i + 2]));
                    }
                    else if (line.Split(' ')[0] == "StretchParamPer")
                    {
                        scaleX = ((100d - double.Parse(line.Split(' ')[1].Replace('.', ',')))) / 100d;
                    }
                    else if (line.Split(' ')[0] == "StretchParamPer_2")
                    {
                        double value = double.Parse(line.Split(' ')[1].Replace('.', ','));
                        scaleY = (100d - value) / 100d;
                        if (value == 0)
                        {
                            scaleY = scaleX;
                        }
                    }
                }
                if (points != null && points.Count > 0)
                {
                    PathFigure path = new PathFigure()
                    {
                        IsClosed = true,
                        StartPoint = points[0]
                    };
                    for (int i = 0; i < points.Count; i += 3)
                    {
                        Point cp = points[i + 1];
                        //Arc
                        if ((cp.X != 0 || cp.Y != 0) && 
                            GCTools.GetArcSegmentFromList(points.Skip(i).Take(3)) is ArcSegment arcSegment)
                        {
                            path.Segments.Add(arcSegment);
                        }
                        // Line
                        else
                        {
                            path.Segments.Add(new LineSegment(points[i + 2], false));
                        }

                    }
                    pathGeometry.Figures.Add(path);
                    ScaleTransform scaleTransform = new ScaleTransform(scaleX, scaleY);
                    pathGeometry.Transform = scaleTransform;
                    room.Add(new CadGeometry(pathGeometry));
                }
                return room;
            }
            return null;
        }

        public static Point ParsePoint(string point_str)
        {
            string[] coord = point_str.Split(new char[] { ' ' }, 2)[1].Split(',');
            return new Point(
                double.Parse(coord[0].Replace('.', ',')), 
                double.Parse(coord[1].Replace('.', ',')));
        }
    }
}
