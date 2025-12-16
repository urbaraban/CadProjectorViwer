using CadProjector.FileFormats.Geometry;
using CadProjector.FileFormats.Geometry.Segments;
using CadProjector.FileFormats.Geometry.Shapes;
using Svg;
using Svg.Pathing;

namespace CadProjector.FileFormats.Parsers
{
    /// <summary>
    /// Парсер SVG файлов.
    /// </summary>
    public class SvgParser : IFileParser
    {
        public string FormatName => "SVG";

        public string[] SupportedExtensions => new[] { ".svg" };

        public bool CanParse(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            return SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<ShapeCollection> ParseAsync(
            string filePath,
            IProgress<ParseProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(filePath))
                {
                    throw new FileParseException($"File not found: {filePath}", filePath);
                }

                try
                {
                    var svgDoc = SvgDocument.Open(filePath);
                    var collection = ParseSvgElements(svgDoc.Children, Path.GetFileName(filePath), progress, cancellationToken);
                    collection.FileName = Path.GetFileName(filePath);
                    collection.Metadata["Source"] = filePath;
                    collection.Units = svgDoc.Width.Type.ToString();

                    return collection;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is not FileParseException)
                {
                    throw new FileParseException($"Failed to parse SVG file: {ex.Message}", filePath, ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Парсит SVG из строки.
        /// </summary>
        public async Task<ShapeCollection> ParseFromStringAsync(
            string svgContent,
            string name = "Clipboard",
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(svgContent);
                using var stream = new MemoryStream(bytes);
                var svgDoc = SvgDocument.Open<SvgDocument>(stream);
                return ParseSvgElements(svgDoc.Children, name, null, cancellationToken);
            }, cancellationToken);
        }

        private ShapeCollection ParseSvgElements(
            SvgElementCollection elements,
            string name,
            IProgress<ParseProgress>? progress,
            CancellationToken cancellationToken)
        {
            var collection = new ShapeCollection(name);
            int total = elements.Count;
            int current = 0;

            foreach (var element in elements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report(new ParseProgress
                {
                    Current = current,
                    Total = total,
                    Message = $"Parsing SVG element {current + 1}/{total}"
                });

                if (element.Visibility?.ToLower() != "hidden")
                {
                    var shape = ParseSvgElement(element, cancellationToken);
                    if (shape != null)
                    {
                        collection.Add(shape);
                    }
                }

                current++;
            }

            return collection;
        }

        private IShape? ParseSvgElement(SvgElement element, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return element switch
            {
                SvgLine line => ParseLine(line),
                SvgCircle circle => ParseCircle(circle),
                SvgEllipse ellipse => ParseEllipse(ellipse),
                SvgRectangle rect => ParseRectangle(rect),
                SvgPolyline polyline => ParsePolyline(polyline),
                SvgPolygon polygon => ParsePolygon(polygon),
                SvgPath path => ParsePath(path),
                SvgText text => ParseText(text),
                SvgGroup group => ParseGroup(group, cancellationToken),
                _ => null
            };
        }

        private LineShape ParseLine(SvgLine line)
        {
            return new LineShape(
                line.StartX.Value, line.StartY.Value,
                line.EndX.Value, line.EndY.Value)
            {
                Id = line.ID
            };
        }

        private CircleShape ParseCircle(SvgCircle circle)
        {
            return new CircleShape(
                circle.CenterX.Value,
                circle.CenterY.Value,
                circle.Radius.Value)
            {
                Id = circle.ID
            };
        }

        private EllipseShape ParseEllipse(SvgEllipse ellipse)
        {
            return new EllipseShape(
                ellipse.CenterX.Value,
                ellipse.CenterY.Value,
                ellipse.RadiusX.Value,
                ellipse.RadiusY.Value)
            {
                Id = ellipse.ID
            };
        }

        private RectangleShape ParseRectangle(SvgRectangle rect)
        {
            return new RectangleShape(
                rect.X.Value,
                rect.Y.Value,
                rect.Width.Value,
                rect.Height.Value,
                rect.CornerRadiusX.Value,
                rect.CornerRadiusY.Value)
            {
                Id = rect.ID
            };
        }

        private PolygonShape ParsePolygon(SvgPolygon polygon)
        {
            var points = new List<Point3D>();
            for (int i = 0; i < polygon.Points.Count - 1; i += 2)
            {
                points.Add(new Point3D(polygon.Points[i], polygon.Points[i + 1]));
            }

            return new PolygonShape(points)
            {
                Id = polygon.ID
            };
        }

        private PolylineShape ParsePolyline(SvgPolyline polyline)
        {
            var points = new List<Point3D>();
            for (int i = 0; i < polyline.Points.Count - 1; i += 2)
            {
                points.Add(new Point3D(polyline.Points[i], polyline.Points[i + 1]));
            }

            return new PolylineShape(points)
            {
                Id = polyline.ID
            };
        }

        private PathShape? ParsePath(SvgPath svgPath)
        {
            if (svgPath.PathData == null || svgPath.PathData.Count == 0)
                return null;

            PathShape? currentPath = null;

            foreach (var segment in svgPath.PathData)
            {
                switch (segment)
                {
                    case SvgMoveToSegment moveTo:
                        if (currentPath != null)
                        {
                            // Если был предыдущий путь, возвращаем его
                            // (упрощение: возвращаем только первый subpath)
                        }
                        currentPath = new PathShape(new Point3D(moveTo.End.X, moveTo.End.Y))
                        {
                            Id = svgPath.ID
                        };
                        break;

                    case SvgLineSegment lineSeg:
                        currentPath?.AddSegment(new Geometry.Segments.LineSegment(
                            new Point3D(lineSeg.End.X, lineSeg.End.Y)));
                        break;

                    case SvgQuadraticCurveSegment quadratic:
                        currentPath?.AddSegment(new QuadraticBezierSegment(
                            new Point3D(quadratic.ControlPoint.X, quadratic.ControlPoint.Y),
                            new Point3D(quadratic.End.X, quadratic.End.Y)));
                        break;

                    case SvgCubicCurveSegment cubic:
                        currentPath?.AddSegment(new CubicBezierSegment(
                            new Point3D(cubic.FirstControlPoint.X, cubic.FirstControlPoint.Y),
                            new Point3D(cubic.SecondControlPoint.X, cubic.SecondControlPoint.Y),
                            new Point3D(cubic.End.X, cubic.End.Y)));
                        break;

                    case SvgArcSegment arc:
                        var sweepDirection = arc.Sweep == SvgArcSweep.Positive
                            ? Geometry.Segments.SweepDirection.Clockwise
                            : Geometry.Segments.SweepDirection.Counterclockwise;

                        currentPath?.AddSegment(new Geometry.Segments.ArcSegment(
                            new Point3D(arc.End.X, arc.End.Y),
                            arc.RadiusX,
                            arc.RadiusY,
                            arc.Angle,
                            arc.Size == SvgArcSize.Large,
                            sweepDirection));
                        break;

                    case SvgClosePathSegment:
                        currentPath?.Close();
                        break;
                }
            }

            return currentPath;
        }

        private TextShape ParseText(SvgText text)
        {
            return new TextShape(
                text.Text ?? string.Empty,
                text.Bounds.X,
                text.Bounds.Y,
                text.FontSize.Value)
            {
                Id = text.ID
            };
        }

        private ShapeGroup ParseGroup(SvgGroup group, CancellationToken cancellationToken)
        {
            var shapeGroup = new ShapeGroup(group.ID ?? "group")
            {
                Id = group.ID
            };

            foreach (var child in group.Children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var shape = ParseSvgElement(child, cancellationToken);
                if (shape != null)
                {
                    shapeGroup.Add(shape);
                }
            }

            return shapeGroup;
        }
    }
}
