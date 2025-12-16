using CadProjector.FileFormats.Geometry;
using CadProjector.FileFormats.Geometry.Segments;
using CadProjector.FileFormats.Geometry.Shapes;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Blocks;

namespace CadProjector.FileFormats.Parsers
{
    /// <summary>
    /// Парсер DXF файлов.
    /// </summary>
    public class DxfParser : IFileParser
    {
        public string FormatName => "DXF";

        public string[] SupportedExtensions => new[] { ".dxf" };

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

                DxfFile dxfFile;
                try
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    dxfFile = DxfFile.Load(fs);
                }
                catch (Exception ex)
                {
                    throw new FileParseException($"Failed to load DXF file: {ex.Message}", filePath, ex);
                }

                var scaleFactor = GetScaleFactor(dxfFile.Header.DefaultDrawingUnits);
                var collection = new ShapeCollection(Path.GetFileName(filePath))
                {
                    Units = dxfFile.Header.DefaultDrawingUnits.ToString()
                };
                collection.Metadata["Source"] = filePath;

                // Получаем слои
                var layers = dxfFile.Layers.Count > 0
                    ? dxfFile.Layers.ToList()
                    : GetLayersFromEntities(dxfFile.Entities);

                int totalLayers = layers.Count;
                int currentLayer = 0;

                foreach (var layer in layers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    progress?.Report(new ParseProgress
                    {
                        Current = currentLayer,
                        Total = totalLayers,
                        Message = $"Parsing layer: {layer.Name}"
                    });

                    var layerGroup = ParseLayer(
                        dxfFile.Entities,
                        dxfFile.Blocks,
                        layer.Name,
                        dxfFile.Header.InsertionBase,
                        scaleFactor,
                        cancellationToken);

                    if (layerGroup.Count > 0)
                    {
                        collection.Add(layerGroup);
                    }

                    currentLayer++;
                }

                return collection;
            }, cancellationToken);
        }

        private static double GetScaleFactor(DxfUnits units)
        {
            return units switch
            {
                DxfUnits.Inches => 25.4,      // 1 inch = 25.4 mm
                DxfUnits.Feet => 304.8,       // 1 foot = 304.8 mm
                DxfUnits.Millimeters => 1.0,
                DxfUnits.Centimeters => 10.0, // 1 cm = 10 mm
                DxfUnits.Meters => 1000.0,    // 1 m = 1000 mm
                _ => 1.0
            };
        }

        private static List<DxfLayer> GetLayersFromEntities(IEnumerable<DxfEntity> entities)
        {
            var layerNames = new HashSet<string>();
            var layers = new List<DxfLayer>();

            foreach (var entity in entities)
            {
                if (!layerNames.Contains(entity.Layer))
                {
                    layerNames.Add(entity.Layer);
                    layers.Add(new DxfLayer(entity.Layer));
                }
            }

            return layers;
        }

        private ShapeGroup ParseLayer(
            IList<DxfEntity> entities,
            IList<DxfBlock> blocks,
            string layerName,
            DxfPoint insertionBase,
            double scaleFactor,
            CancellationToken cancellationToken)
        {
            var group = new ShapeGroup(layerName)
            {
                Name = layerName
            };

            var layerEntities = entities.Where(e => e.Layer == layerName);

            foreach (var entity in layerEntities)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entity is DxfInsert insert)
                {
                    // Обработка вставленных блоков
                    var block = blocks.FirstOrDefault(b => b.Name == insert.Name);
                    if (block != null)
                    {
                        var insertLocation = new DxfPoint(
                            insertionBase.X + insert.Location.X,
                            insertionBase.Y + insert.Location.Y,
                            insertionBase.Z + insert.Location.Z);

                        var blockGroup = ParseBlockEntities(
                            block.Entities,
                            blocks,
                            insert.Name,
                            insertLocation,
                            scaleFactor,
                            cancellationToken);

                        if (blockGroup.Count > 0)
                        {
                            group.Add(blockGroup);
                        }
                    }
                }
                else
                {
                    var shape = ParseEntity(entity, insertionBase, scaleFactor);
                    if (shape != null)
                    {
                        group.Add(shape);
                    }
                }
            }

            return group;
        }

        private ShapeGroup ParseBlockEntities(
            IList<DxfEntity> entities,
            IList<DxfBlock> blocks,
            string blockName,
            DxfPoint location,
            double scaleFactor,
            CancellationToken cancellationToken)
        {
            var group = new ShapeGroup(blockName);

            foreach (var entity in entities)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var shape = ParseEntity(entity, location, scaleFactor);
                if (shape != null)
                {
                    group.Add(shape);
                }
            }

            return group;
        }

        private IShape? ParseEntity(DxfEntity entity, DxfPoint location, double scaleFactor)
        {
            return entity switch
            {
                DxfLine line => ParseLine(line, location, scaleFactor),
                DxfArc arc => ParseArc(arc, location, scaleFactor),
                DxfCircle circle => ParseCircle(circle, location, scaleFactor),
                DxfEllipse ellipse => ParseEllipse(ellipse, location, scaleFactor),
                DxfLwPolyline lwPolyline => ParseLwPolyline(lwPolyline, scaleFactor),
                DxfPolyline polyline => ParsePolyline(polyline, location, scaleFactor),
                DxfSpline spline => ParseSpline(spline, location, scaleFactor),
                DxfText text => ParseText(text, location, scaleFactor),
                DxfMText mText => ParseMText(mText, location, scaleFactor),
                _ => null
            };
        }

        private LineShape ParseLine(DxfLine line, DxfPoint location, double scaleFactor)
        {
            return new LineShape(
                (line.P1.X + location.X) * scaleFactor,
                -(line.P1.Y + location.Y) * scaleFactor,
                (line.P2.X + location.X) * scaleFactor,
                -(line.P2.Y + location.Y) * scaleFactor);
        }

        private CircleShape ParseCircle(DxfCircle circle, DxfPoint location, double scaleFactor)
        {
            return new CircleShape(
                (circle.Center.X + location.X) * scaleFactor,
                -(circle.Center.Y + location.Y) * scaleFactor,
                circle.Radius * scaleFactor);
        }

        private PathShape ParseArc(DxfArc arc, DxfPoint location, double scaleFactor)
        {
            var startPoint = arc.GetPointFromAngle(arc.StartAngle);
            var endPoint = arc.GetPointFromAngle(arc.EndAngle);

            var path = new PathShape(new Point3D(
                (startPoint.X + location.X) * scaleFactor,
                -(startPoint.Y + location.Y) * scaleFactor));

            double deltaAngle = (360 + arc.EndAngle - arc.StartAngle) % 360;
            var sweepDirection = arc.Normal.Z < 0
                ? Geometry.Segments.SweepDirection.Clockwise
                : Geometry.Segments.SweepDirection.Counterclockwise;

            path.AddSegment(new ArcSegment(
                new Point3D(
                    (endPoint.X + location.X) * scaleFactor,
                    -(endPoint.Y + location.Y) * scaleFactor),
                arc.Radius * scaleFactor,
                arc.Radius * scaleFactor,
                0,
                deltaAngle > 180,
                sweepDirection));

            return path;
        }

        private EllipseShape ParseEllipse(DxfEllipse ellipse, DxfPoint location, double scaleFactor)
        {
            double majorAngle = Math.Atan2(ellipse.MajorAxis.Y, ellipse.MajorAxis.X) * 180 / Math.PI;

            return new EllipseShape(
                (ellipse.Center.X + location.X) * scaleFactor,
                -(ellipse.Center.Y + location.Y) * scaleFactor,
                ellipse.MajorAxis.Length * scaleFactor,
                ellipse.MajorAxis.Length * ellipse.MinorAxisRatio * scaleFactor,
                majorAngle);
        }

        private PathShape ParseLwPolyline(DxfLwPolyline polyline, double scaleFactor)
        {
            if (polyline.Vertices.Count == 0)
                return new PathShape(Point3D.Zero);

            var firstVertex = polyline.Vertices[0];
            var path = new PathShape(new Point3D(
                firstVertex.X * scaleFactor,
                -firstVertex.Y * scaleFactor));

            for (int i = 0; i < polyline.Vertices.Count - 1; i++)
            {
                var v1 = polyline.Vertices[i];
                var v2 = polyline.Vertices[i + 1];

                var point1 = new Point3D(v1.X * scaleFactor, -v1.Y * scaleFactor);
                var point2 = new Point3D(v2.X * scaleFactor, -v2.Y * scaleFactor);

                if (Math.Abs(v1.Bulge) > 1e-10)
                {
                    // Дуга
                    double bulge = v1.Bulge;
                    double radian = Math.Abs(Math.Atan(bulge)) * 4;
                    double radius = CalculateBulgeRadius(point1, point2, bulge) * scaleFactor;

                    var direction = bulge < 0
                        ? Geometry.Segments.SweepDirection.Clockwise
                        : Geometry.Segments.SweepDirection.Counterclockwise;

                    double angleDegrees = radian * 180 / Math.PI;
                    bool isLargeArc = angleDegrees > 180;

                    path.AddSegment(new ArcSegment(point2, radius, radius, 0, isLargeArc, direction));
                }
                else
                {
                    // Линия
                    path.AddSegment(new Geometry.Segments.LineSegment(point2));
                }
            }

            if (polyline.IsClosed)
            {
                path.Close();
            }

            return path;
        }

        private IShape ParsePolyline(DxfPolyline polyline, DxfPoint location, double scaleFactor)
        {
            var points = new List<Point3D>();

            foreach (var vertex in polyline.Vertices)
            {
                points.Add(new Point3D(
                    (vertex.Location.X + location.X) * scaleFactor,
                    -(vertex.Location.Y + location.Y) * scaleFactor,
                    (vertex.Location.Z + location.Z) * scaleFactor));
            }

            if (polyline.IsClosed && points.Count > 0)
            {
                return new PolygonShape(points);
            }

            return new PolylineShape(points);
        }

        private IShape ParseSpline(DxfSpline spline, DxfPoint location, double scaleFactor)
        {
            if (spline.ControlPoints.Count == 0)
                return new PathShape(Point3D.Zero);

            // Преобразуем контрольные точки DXF в NurbsControlPoint
            var controlPoints = spline.ControlPoints.Select(cp =>
            {
                double x = (cp.Point.X + location.X) * scaleFactor;
                double y = -(cp.Point.Y + location.Y) * scaleFactor;
                double z = (cp.Point.Z + location.Z) * scaleFactor;
                return new NurbsControlPoint(x, y, z, cp.Weight);
            });

            // Создаём NurbsShape
            var nurbs = new NurbsShape(
                controlPoints,
                spline.DegreeOfCurve,
                spline.KnotValues,
                spline.IsRational);

            // Возвращаем оптимизированный путь с дугами и линиями
            return nurbs.ToOptimizedPath(1.0);
        }

        private TextShape ParseText(DxfText text, DxfPoint location, double scaleFactor)
        {
            return new TextShape(
                text.Value ?? string.Empty,
                (text.Location.X + location.X) * scaleFactor,
                -(text.Location.Y + location.Y) * scaleFactor,
                text.TextHeight * scaleFactor);
        }

        private TextShape ParseMText(DxfMText mText, DxfPoint location, double scaleFactor)
        {
            // Убираем форматирование из MText
            string text = System.Text.RegularExpressions.Regex.Replace(
                mText.Text ?? string.Empty,
                @"\\[A-Za-z];|\\[A-Za-z]\d+;|\{|\}",
                "");

            return new TextShape(
                text,
                (mText.InsertionPoint.X + location.X) * scaleFactor,
                -(mText.InsertionPoint.Y + location.Y) * scaleFactor,
                mText.InitialTextHeight * scaleFactor);
        }

        private static double CalculateBulgeRadius(Point3D point1, Point3D point2, double bulge)
        {
            double angle = Math.Atan(bulge) * 4;
            double distance = point1.DistanceTo2D(point2);
            double radius = (distance / 2) / Math.Sin(angle / 2);

            return double.IsInfinity(radius) ? 0 : Math.Abs(radius);
        }
    }
}
