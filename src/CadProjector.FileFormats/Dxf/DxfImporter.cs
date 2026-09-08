using CadProjector.Core.Scene;
using CadProjector.Geometry.Primitives;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace CadProjector.FileFormats.Dxf;

public sealed class DxfImporter : IDrawingImporter
{
    public IReadOnlyList<string> Extensions { get; } = [".dxf"];
    public bool FlipY { get; set; } = true;
    public bool GroupByLayer { get; set; } = true;

    public bool CanImport(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public Task<ImportResult> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var fs = File.OpenRead(path);
            var dxf = DxfFile.Load(fs);
            var scale = UnitsToMm(dxf.Header.DefaultDrawingUnits);
            var layerOn = dxf.Layers
                .GroupBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().IsLayerOn, StringComparer.OrdinalIgnoreCase);

            bool IsLayerVisible(string? name)
            {
                if (string.IsNullOrEmpty(name)) return true;
                return !layerOn.TryGetValue(name, out var on) || on;
            }

            var raw = new List<(string Layer, uint? Color, List<List<Point2>> Contours, string Name, bool Visible)>();

            void AddEntity(DxfEntity entity, double ox, double oy, double rotDeg, double sx, double sy)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var contours = EntityToContours(entity, scale);
                if (contours.Count == 0)
                    return;
                contours = TransformContours(contours, ox, oy, rotDeg, sx, sy);
                raw.Add((entity.Layer, ToArgb(entity.Color), contours, $"{entity.EntityType}:{entity.Layer}", IsLayerVisible(entity.Layer)));
            }

            foreach (var entity in dxf.Entities)
            {
                if (entity is DxfInsert insert)
                {
                    var block = dxf.Blocks.FirstOrDefault(b =>
                        string.Equals(b.Name, insert.Name, StringComparison.OrdinalIgnoreCase));
                    if (block is null)
                        continue;

                    var ix = insert.Location.X * scale;
                    var iy = insert.Location.Y * scale;
                    var rot = insert.Rotation;
                    var xs = insert.XScaleFactor == 0 ? 1 : insert.XScaleFactor;
                    var ys = insert.YScaleFactor == 0 ? 1 : insert.YScaleFactor;
                    foreach (var be in block.Entities)
                        AddEntity(be, ix, iy, rot, xs, ys);
                    continue;
                }

                AddEntity(entity, 0, 0, 0, 1, 1);
            }

            if (FlipY && raw.Count > 0)
            {
                var maxY = raw.SelectMany(r => r.Contours).SelectMany(c => c).DefaultIfEmpty(Point2.Zero).Max(p => p.Y);
                raw = raw.Select(r => (
                    r.Layer,
                    r.Color,
                    r.Contours.Select(c => c.Select(p => new Point2(p.X, maxY - p.Y)).ToList()).ToList(),
                    r.Name,
                    r.Visible
                )).ToList();
            }

            List<Drawable> drawables;
            if (GroupByLayer)
            {
                drawables = raw
                    .GroupBy(r => r.Layer ?? "0")
                    .Select(g => new Drawable
                    {
                        Name = $"Layer:{g.Key}",
                        LayerName = g.Key,
                        ColorArgb = g.Select(x => x.Color).FirstOrDefault(c => c is not null),
                        IsVisible = g.Any(x => x.Visible) && IsLayerVisible(g.Key),
                        Contours = g.SelectMany(x => x.Contours).ToList()
                    })
                    .Where(d => d.Contours.Count > 0)
                    .ToList();
            }
            else
            {
                drawables = raw.Select(r => new Drawable
                {
                    Name = r.Name,
                    LayerName = r.Layer,
                    ColorArgb = r.Color,
                    IsVisible = r.Visible,
                    Contours = r.Contours
                }).ToList();
            }

            return new ImportResult
            {
                SourcePath = path,
                Format = "DXF",
                Drawables = drawables
            };
        }, cancellationToken);
    }

    private static List<List<Point2>> TransformContours(
        List<List<Point2>> contours, double ox, double oy, double rotDeg, double sx, double sy)
    {
        if (Math.Abs(ox) < 1e-12 && Math.Abs(oy) < 1e-12 &&
            Math.Abs(rotDeg) < 1e-12 && Math.Abs(sx - 1) < 1e-12 && Math.Abs(sy - 1) < 1e-12)
            return contours;

        var rad = rotDeg * Math.PI / 180.0;
        var c = Math.Cos(rad);
        var s = Math.Sin(rad);
        return contours.Select(contour => contour.Select(p =>
        {
            var x = p.X * sx;
            var y = p.Y * sy;
            var rx = x * c - y * s;
            var ry = x * s + y * c;
            return new Point2(rx + ox, ry + oy);
        }).ToList()).ToList();
    }

    private static double UnitsToMm(DxfUnits units) => units switch
    {
        DxfUnits.Inches => 25.4,
        DxfUnits.Feet => 304.8,
        DxfUnits.Millimeters => 1.0,
        DxfUnits.Centimeters => 10.0,
        DxfUnits.Meters => 1000.0,
        _ => 1.0
    };

    private static uint? ToArgb(DxfColor color)
    {
        try
        {
            var rgb = color.ToRGB();
            var r = (byte)((rgb >> 16) & 0xFF);
            var g = (byte)((rgb >> 8) & 0xFF);
            var b = (byte)(rgb & 0xFF);
            if (r == 0 && g == 0 && b == 0 && color.IsByLayer)
                return null;
            return 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
        }
        catch
        {
            return null;
        }
    }

    private static List<List<Point2>> EntityToContours(DxfEntity entity, double scale)
    {
        if (entity is DxfLine line)
        {
            return
            [
                [
                    new Point2(line.P1.X * scale, line.P1.Y * scale),
                    new Point2(line.P2.X * scale, line.P2.Y * scale)
                ]
            ];
        }

        if (entity is DxfLwPolyline lw)
        {
            var pts = lw.Vertices.Select(v => new Point2(v.X * scale, v.Y * scale)).ToList();
            if (lw.IsClosed && pts.Count > 1)
                pts.Add(pts[0]);
            return pts.Count >= 2 ? [pts] : [];
        }

        if (entity is DxfPolyline poly)
        {
            var pts = poly.Vertices.Select(v => new Point2(v.Location.X * scale, v.Location.Y * scale)).ToList();
            if (poly.IsClosed && pts.Count > 1)
                pts.Add(pts[0]);
            return pts.Count >= 2 ? [pts] : [];
        }

        if (entity is DxfCircle circle)
            return [SampleEllipse(circle.Center.X * scale, circle.Center.Y * scale, circle.Radius * scale, circle.Radius * scale, 0, 64)];

        if (entity is DxfArc arc)
            return [SampleArc(arc.Center.X * scale, arc.Center.Y * scale, arc.Radius * scale, arc.StartAngle, arc.EndAngle, 48)];

        if (entity is DxfEllipse ellipse)
        {
            var cx = ellipse.Center.X * scale;
            var cy = ellipse.Center.Y * scale;
            var mx = ellipse.MajorAxis.X * scale;
            var my = ellipse.MajorAxis.Y * scale;
            var major = Math.Sqrt(mx * mx + my * my);
            var minor = major * ellipse.MinorAxisRatio;
            var rot = Math.Atan2(my, mx) * 180.0 / Math.PI;
            var start = ellipse.StartParameter * 180.0 / Math.PI;
            var end = ellipse.EndParameter * 180.0 / Math.PI;
            return [SampleEllipse(cx, cy, major, minor, rot, 64, start, end)];
        }

        if (entity is DxfSpline spline)
        {
            var ctrl = spline.ControlPoints.Select(p => new Point2(p.Point.X * scale, p.Point.Y * scale)).ToList();
            if (ctrl.Count >= 2)
                return [SamplePolylineDense(ctrl, 4)];
        }

        return [];
    }

    private static List<Point2> SamplePolylineDense(List<Point2> ctrl, int subdiv)
    {
        if (ctrl.Count < 2) return ctrl;
        var pts = new List<Point2>();
        for (var i = 0; i < ctrl.Count - 1; i++)
        {
            for (var s = 0; s < subdiv; s++)
            {
                var t = (double)s / subdiv;
                pts.Add(new Point2(
                    ctrl[i].X + (ctrl[i + 1].X - ctrl[i].X) * t,
                    ctrl[i].Y + (ctrl[i + 1].Y - ctrl[i].Y) * t));
            }
        }
        pts.Add(ctrl[^1]);
        return pts;
    }

    private static List<Point2> SampleEllipse(
        double cx, double cy, double rx, double ry, double rotDeg, int n,
        double startDeg = 0, double endDeg = 360)
    {
        var start = startDeg * Math.PI / 180.0;
        var end = endDeg * Math.PI / 180.0;
        if (end < start) end += 2 * Math.PI;
        var rot = rotDeg * Math.PI / 180.0;
        var cr = Math.Cos(rot);
        var sr = Math.Sin(rot);
        var pts = new List<Point2>(n + 1);
        for (var i = 0; i <= n; i++)
        {
            var t = (double)i / n;
            var a = start + (end - start) * t;
            var lx = rx * Math.Cos(a);
            var ly = ry * Math.Sin(a);
            pts.Add(new Point2(cx + lx * cr - ly * sr, cy + lx * sr + ly * cr));
        }
        return pts;
    }

    private static List<Point2> SampleArc(double cx, double cy, double r, double startDeg, double endDeg, int n)
    {
        var start = startDeg * Math.PI / 180.0;
        var end = endDeg * Math.PI / 180.0;
        if (end < start) end += 2 * Math.PI;
        var pts = new List<Point2>(n + 1);
        for (var i = 0; i <= n; i++)
        {
            var t = (double)i / n;
            var a = start + (end - start) * t;
            pts.Add(new Point2(cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
        }
        return pts;
    }
}
