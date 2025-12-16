using CadProjector.FileFormats.Geometry;
using CadProjector.FileFormats.Geometry.Segments;
using CadProjector.FileFormats.Geometry.Shapes;
using System.Globalization;
using System.Text;
using System.Xml;

namespace CadProjector.FileFormats.Export
{
    /// <summary>
    /// Экспортёр в SVG формат.
    /// </summary>
    public class SvgExporter : IExporter
    {
        public string FormatName => "SVG";

        public string[] SupportedExtensions => new[] { ".svg" };

        public async Task ExportAsync(
            ShapeCollection shapes,
            string filePath,
            ExportOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new ExportOptions();

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bounds = shapes.Bounds;
                double width = bounds.Width * options.Scale;
                double height = bounds.Height * options.Scale;

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = Encoding.UTF8,
                    Async = true
                };

                using var writer = XmlWriter.Create(filePath, settings);
                writer.WriteStartDocument();
                writer.WriteStartElement("svg", "http://www.w3.org/2000/svg");
                writer.WriteAttributeString("version", "1.1");
                writer.WriteAttributeString("width", FormatNumber(width) + options.Units);
                writer.WriteAttributeString("height", FormatNumber(height) + options.Units);
                writer.WriteAttributeString("viewBox", 
                    $"{FormatNumber(bounds.Min.X)} {FormatNumber(bounds.Min.Y)} {FormatNumber(bounds.Width)} {FormatNumber(bounds.Height)}");

                if (options.IncludeMetadata && shapes.FileName != null)
                {
                    writer.WriteComment($" Generated from: {shapes.FileName} ");
                }

                foreach (var shape in shapes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteShape(writer, shape, options);
                }

                writer.WriteEndElement(); // svg
                writer.WriteEndDocument();
            }, cancellationToken);
        }

        private void WriteShape(XmlWriter writer, IShape shape, ExportOptions options)
        {
            switch (shape)
            {
                case LineShape line:
                    WriteLine(writer, line);
                    break;
                case CircleShape circle:
                    WriteCircle(writer, circle);
                    break;
                case EllipseShape ellipse:
                    WriteEllipse(writer, ellipse);
                    break;
                case RectangleShape rect:
                    WriteRectangle(writer, rect);
                    break;
                case PolygonShape polygon:
                    WritePolygon(writer, polygon);
                    break;
                case PolylineShape polyline:
                    WritePolyline(writer, polyline);
                    break;
                case PathShape path:
                    WritePath(writer, path);
                    break;
                case TextShape text:
                    WriteText(writer, text);
                    break;
                case ShapeGroup group:
                    WriteGroup(writer, group, options);
                    break;
            }
        }

        private void WriteLine(XmlWriter writer, LineShape line)
        {
            writer.WriteStartElement("line");
            if (line.Id != null) writer.WriteAttributeString("id", line.Id);
            writer.WriteAttributeString("x1", FormatNumber(line.Start.X));
            writer.WriteAttributeString("y1", FormatNumber(line.Start.Y));
            writer.WriteAttributeString("x2", FormatNumber(line.End.X));
            writer.WriteAttributeString("y2", FormatNumber(line.End.Y));
            writer.WriteAttributeString("stroke", "black");
            writer.WriteAttributeString("fill", "none");
            writer.WriteEndElement();
        }

        private void WriteCircle(XmlWriter writer, CircleShape circle)
        {
            writer.WriteStartElement("circle");
            if (circle.Id != null) writer.WriteAttributeString("id", circle.Id);
            writer.WriteAttributeString("cx", FormatNumber(circle.Center.X));
            writer.WriteAttributeString("cy", FormatNumber(circle.Center.Y));
            writer.WriteAttributeString("r", FormatNumber(circle.Radius));
            writer.WriteAttributeString("stroke", "black");
            writer.WriteAttributeString("fill", "none");
            writer.WriteEndElement();
        }

        private void WriteEllipse(XmlWriter writer, EllipseShape ellipse)
        {
            writer.WriteStartElement("ellipse");
            if (ellipse.Id != null) writer.WriteAttributeString("id", ellipse.Id);
            writer.WriteAttributeString("cx", FormatNumber(ellipse.Center.X));
            writer.WriteAttributeString("cy", FormatNumber(ellipse.Center.Y));
            writer.WriteAttributeString("rx", FormatNumber(ellipse.RadiusX));
            writer.WriteAttributeString("ry", FormatNumber(ellipse.RadiusY));
            if (Math.Abs(ellipse.Rotation) > 0.01)
            {
                writer.WriteAttributeString("transform", 
                    $"rotate({FormatNumber(ellipse.Rotation)} {FormatNumber(ellipse.Center.X)} {FormatNumber(ellipse.Center.Y)})");
            }
            writer.WriteAttributeString("stroke", "black");
            writer.WriteAttributeString("fill", "none");
            writer.WriteEndElement();
        }

        private void WriteRectangle(XmlWriter writer, RectangleShape rect)
        {
            writer.WriteStartElement("rect");
            if (rect.Id != null) writer.WriteAttributeString("id", rect.Id);
            writer.WriteAttributeString("x", FormatNumber(rect.X));
            writer.WriteAttributeString("y", FormatNumber(rect.Y));
            writer.WriteAttributeString("width", FormatNumber(rect.Width));
            writer.WriteAttributeString("height", FormatNumber(rect.Height));
            if (rect.RadiusX > 0) writer.WriteAttributeString("rx", FormatNumber(rect.RadiusX));
            if (rect.RadiusY > 0) writer.WriteAttributeString("ry", FormatNumber(rect.RadiusY));
            writer.WriteAttributeString("stroke", "black");
            writer.WriteAttributeString("fill", "none");
            writer.WriteEndElement();
        }

        private void WritePolygon(XmlWriter writer, PolygonShape polygon)
        {
            writer.WriteStartElement("polygon");
            if (polygon.Id != null) writer.WriteAttributeString("id", polygon.Id);
            writer.WriteAttributeString("points", 
                string.Join(" ", polygon.Points.Select(p => $"{FormatNumber(p.X)},{FormatNumber(p.Y)}")));
            writer.WriteAttributeString("stroke", "black");
            writer.WriteAttributeString("fill", "none");
            writer.WriteEndElement();
        }

        private void WritePolyline(XmlWriter writer, PolylineShape polyline)
        {
            writer.WriteStartElement("polyline");
            if (polyline.Id != null) writer.WriteAttributeString("id", polyline.Id);
            writer.WriteAttributeString("points", 
                string.Join(" ", polyline.Points.Select(p => $"{FormatNumber(p.X)},{FormatNumber(p.Y)}")));
            writer.WriteAttributeString("stroke", "black");
            writer.WriteAttributeString("fill", "none");
            writer.WriteEndElement();
        }

        private void WritePath(XmlWriter writer, PathShape path)
        {
            writer.WriteStartElement("path");
            if (path.Id != null) writer.WriteAttributeString("id", path.Id);
            writer.WriteAttributeString("d", BuildPathData(path));
            writer.WriteAttributeString("stroke", "black");
            writer.WriteAttributeString("fill", "none");
            writer.WriteEndElement();
        }

        private string BuildPathData(PathShape path)
        {
            var sb = new StringBuilder();
            sb.Append($"M {FormatNumber(path.StartPoint.X)},{FormatNumber(path.StartPoint.Y)}");

            foreach (var segment in path.Segments)
            {
                switch (segment)
                {
                    case Geometry.Segments.LineSegment line:
                        sb.Append($" L {FormatNumber(line.EndPoint.X)},{FormatNumber(line.EndPoint.Y)}");
                        break;

                    case QuadraticBezierSegment qBez:
                        sb.Append($" Q {FormatNumber(qBez.ControlPoint.X)},{FormatNumber(qBez.ControlPoint.Y)} " +
                                  $"{FormatNumber(qBez.EndPoint.X)},{FormatNumber(qBez.EndPoint.Y)}");
                        break;

                    case CubicBezierSegment cBez:
                        sb.Append($" C {FormatNumber(cBez.ControlPoint1.X)},{FormatNumber(cBez.ControlPoint1.Y)} " +
                                  $"{FormatNumber(cBez.ControlPoint2.X)},{FormatNumber(cBez.ControlPoint2.Y)} " +
                                  $"{FormatNumber(cBez.EndPoint.X)},{FormatNumber(cBez.EndPoint.Y)}");
                        break;

                    case Geometry.Segments.ArcSegment arc:
                        sb.Append($" A {FormatNumber(arc.RadiusX)},{FormatNumber(arc.RadiusY)} " +
                                  $"{FormatNumber(arc.RotationAngle)} " +
                                  $"{(arc.IsLargeArc ? 1 : 0)},{(arc.SweepDirection == Geometry.Segments.SweepDirection.Clockwise ? 1 : 0)} " +
                                  $"{FormatNumber(arc.EndPoint.X)},{FormatNumber(arc.EndPoint.Y)}");
                        break;

                    case CloseSegment:
                        sb.Append(" Z");
                        break;
                }
            }

            return sb.ToString();
        }

        private void WriteText(XmlWriter writer, TextShape text)
        {
            writer.WriteStartElement("text");
            if (text.Id != null) writer.WriteAttributeString("id", text.Id);
            writer.WriteAttributeString("x", FormatNumber(text.Position.X));
            writer.WriteAttributeString("y", FormatNumber(text.Position.Y));
            writer.WriteAttributeString("font-family", text.FontFamily);
            writer.WriteAttributeString("font-size", FormatNumber(text.FontSize));
            writer.WriteAttributeString("fill", "black");
            writer.WriteString(text.Text);
            writer.WriteEndElement();
        }

        private void WriteGroup(XmlWriter writer, ShapeGroup group, ExportOptions options)
        {
            writer.WriteStartElement("g");
            if (group.Id != null) writer.WriteAttributeString("id", group.Id);

            foreach (var shape in group)
            {
                WriteShape(writer, shape, options);
            }

            writer.WriteEndElement();
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("F3", CultureInfo.InvariantCulture);
        }
    }
}

