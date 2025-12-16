using CadProjector.FileFormats.Geometry;
using CadProjector.FileFormats.Geometry.Shapes;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace CadProjector.FileFormats.Export
{
    /// <summary>
    /// Экспортёр в DXF формат.
    /// </summary>
    public class DxfExporter : IExporter
    {
        public string FormatName => "DXF";

        public string[] SupportedExtensions => new[] { ".dxf" };

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

                var dxfFile = new DxfFile();

                // Настройка единиц измерения
                dxfFile.Header.DefaultDrawingUnits = options.Units.ToLower() switch
                {
                    "mm" or "millimeters" => DxfUnits.Millimeters,
                    "cm" or "centimeters" => DxfUnits.Centimeters,
                    "m" or "meters" => DxfUnits.Meters,
                    "in" or "inches" => DxfUnits.Inches,
                    "ft" or "feet" => DxfUnits.Feet,
                    _ => DxfUnits.Millimeters
                };

                // Метаданные в DXF ограничены, пропускаем

                // Экспортируем фигуры
                foreach (var shape in shapes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ExportShape(dxfFile, shape, options, "0");
                }

                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                dxfFile.Save(fs);
            }, cancellationToken);
        }

        private void ExportShape(DxfFile dxfFile, IShape shape, ExportOptions options, string layerName)
        {
            switch (shape)
            {
                case LineShape line:
                    ExportLine(dxfFile, line, layerName);
                    break;
                case CircleShape circle:
                    ExportCircle(dxfFile, circle, layerName);
                    break;
                case EllipseShape ellipse:
                    ExportEllipse(dxfFile, ellipse, layerName);
                    break;
                case RectangleShape rect:
                    ExportRectangle(dxfFile, rect, options, layerName);
                    break;
                case PolygonShape polygon:
                    ExportPolygon(dxfFile, polygon, layerName);
                    break;
                case PolylineShape polyline:
                    ExportPolyline(dxfFile, polyline, layerName);
                    break;
                case NurbsShape nurbs:
                    ExportNurbs(dxfFile, nurbs, layerName);
                    break;
                case PathShape path:
                    ExportPath(dxfFile, path, options, layerName);
                    break;
                case TextShape text:
                    ExportText(dxfFile, text, layerName);
                    break;
                case ShapeGroup group:
                    ExportGroup(dxfFile, group, options);
                    break;
            }
        }

        private void ExportLine(DxfFile dxfFile, LineShape line, string layerName)
        {
            var dxfLine = new DxfLine(
                new DxfPoint(line.Start.X, -line.Start.Y, line.Start.Z),
                new DxfPoint(line.End.X, -line.End.Y, line.End.Z))
            {
                Layer = layerName
            };
            dxfFile.Entities.Add(dxfLine);
        }

        private void ExportCircle(DxfFile dxfFile, CircleShape circle, string layerName)
        {
            var dxfCircle = new DxfCircle(
                new DxfPoint(circle.Center.X, -circle.Center.Y, circle.Center.Z),
                circle.Radius)
            {
                Layer = layerName
            };
            dxfFile.Entities.Add(dxfCircle);
        }

        private void ExportEllipse(DxfFile dxfFile, EllipseShape ellipse, string layerName)
        {
            double rotationRad = ellipse.Rotation * Math.PI / 180;
            var majorAxis = new DxfVector(
                ellipse.RadiusX * Math.Cos(rotationRad),
                -ellipse.RadiusX * Math.Sin(rotationRad),
                0);

            var dxfEllipse = new DxfEllipse(
                new DxfPoint(ellipse.Center.X, -ellipse.Center.Y, ellipse.Center.Z),
                majorAxis,
                ellipse.RadiusY / ellipse.RadiusX)
            {
                Layer = layerName
            };
            dxfFile.Entities.Add(dxfEllipse);
        }

        private void ExportRectangle(DxfFile dxfFile, RectangleShape rect, ExportOptions options, string layerName)
        {
            // Экспортируем как полилинию
            var points = rect.Tessellate(options.TessellationTolerance);
            var vertices = points.Take(points.Count - 1) // Убираем дублирующую последнюю точку
                .Select(p => new DxfLwPolylineVertex { X = p.X, Y = -p.Y })
                .ToList();

            var polyline = new DxfLwPolyline(vertices)
            {
                Layer = layerName,
                IsClosed = true
            };

            dxfFile.Entities.Add(polyline);
        }

        private void ExportPolygon(DxfFile dxfFile, PolygonShape polygon, string layerName)
        {
            var vertices = polygon.Points
                .Select(p => new DxfLwPolylineVertex { X = p.X, Y = -p.Y })
                .ToList();

            var polyline = new DxfLwPolyline(vertices)
            {
                Layer = layerName,
                IsClosed = true
            };

            dxfFile.Entities.Add(polyline);
        }

        private void ExportPolyline(DxfFile dxfFile, PolylineShape polyline, string layerName)
        {
            var vertices = polyline.Points
                .Select(p => new DxfLwPolylineVertex { X = p.X, Y = -p.Y })
                .ToList();

            var dxfPolyline = new DxfLwPolyline(vertices)
            {
                Layer = layerName,
                IsClosed = false
            };

            dxfFile.Entities.Add(dxfPolyline);
        }

        private void ExportPath(DxfFile dxfFile, PathShape path, ExportOptions options, string layerName)
        {
            // Тесселируем путь и экспортируем как полилинию
            var points = path.Tessellate(options.TessellationTolerance);

            if (points.Count < 2)
                return;

            var pointList = points.ToList();
            // Если замкнут, убираем последнюю дублирующую точку
            if (path.IsClosed && pointList.Count > 1 &&
                pointList[0].DistanceTo(pointList[^1]) < 0.001)
            {
                pointList.RemoveAt(pointList.Count - 1);
            }

            var vertices = pointList
                .Select(p => new DxfLwPolylineVertex { X = p.X, Y = -p.Y })
                .ToList();

            var polyline = new DxfLwPolyline(vertices)
            {
                Layer = layerName,
                IsClosed = path.IsClosed
            };

            dxfFile.Entities.Add(polyline);
        }

        private void ExportText(DxfFile dxfFile, TextShape text, string layerName)
        {
            var dxfText = new DxfText(
                new DxfPoint(text.Position.X, -text.Position.Y, text.Position.Z),
                text.FontSize,
                text.Text)
            {
                Layer = layerName
            };
            dxfFile.Entities.Add(dxfText);
        }

        private void ExportNurbs(DxfFile dxfFile, NurbsShape nurbs, string layerName)
        {
            // Создаём DxfSpline из NurbsShape
            var spline = new DxfSpline
            {
                Layer = layerName,
                DegreeOfCurve = nurbs.Degree,
                IsRational = nurbs.IsRational
            };

            // Добавляем контрольные точки
            foreach (var cp in nurbs.ControlPoints)
            {
                spline.ControlPoints.Add(
                    new DxfControlPoint(new DxfPoint(cp.X, -cp.Y, cp.Z), cp.Weight));
            }

            // Копируем узловой вектор
            foreach (var knot in nurbs.KnotVector)
            {
                spline.KnotValues.Add(knot);
            }

            dxfFile.Entities.Add(spline);
        }

        private void ExportGroup(DxfFile dxfFile, ShapeGroup group, ExportOptions options)
        {
            // Используем имя группы как имя слоя
            string layerName = group.Name ?? group.Id ?? "0";

            // Создаём слой если его нет
            if (!dxfFile.Layers.Any(l => l.Name == layerName))
            {
                dxfFile.Layers.Add(new DxfLayer(layerName));
            }

            foreach (var shape in group)
            {
                ExportShape(dxfFile, shape, options, layerName);
            }
        }
    }
}

