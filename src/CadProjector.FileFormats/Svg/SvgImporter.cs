using System.Drawing;
using CadProjector.Core.Scene;
using CadProjector.Geometry.Primitives;
using Svg;
using Svg.Pathing;

namespace CadProjector.FileFormats.Svg;

public sealed class SvgImporter : IDrawingImporter
{
    public IReadOnlyList<string> Extensions { get; } = [".svg"];

    public bool CanImport(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public Task<ImportResult> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var doc = SvgDocument.Open(path);
            var drawables = new List<Drawable>();
            Walk(doc.Children, drawables, cancellationToken);

            return new ImportResult
            {
                SourcePath = path,
                Format = "SVG",
                Drawables = drawables
            };
        }, cancellationToken);
    }

    private static void Walk(SvgElementCollection elements, List<Drawable> sink, CancellationToken ct)
    {
        foreach (var el in elements)
        {
            ct.ThrowIfCancellationRequested();
            switch (el)
            {
                case SvgGroup group:
                    Walk(group.Children, sink, ct);
                    break;
                case SvgLine line:
                    sink.Add(Make(line.ID ?? "line",
                    [
                        [
                            new Point2(line.StartX.Value, line.StartY.Value),
                            new Point2(line.EndX.Value, line.EndY.Value)
                        ]
                    ], line.Stroke));
                    break;
                case SvgPolyline poly:
                {
                    var pts = FlatPoints(poly.Points);
                    if (pts.Count >= 2)
                        sink.Add(Make(poly.ID ?? "polyline", [pts], poly.Stroke));
                    break;
                }
                case SvgPolygon polygon:
                {
                    var pts = FlatPoints(polygon.Points);
                    if (pts.Count >= 2)
                    {
                        pts.Add(pts[0]);
                        sink.Add(Make(polygon.ID ?? "polygon", [pts], polygon.Stroke));
                    }
                    break;
                }
                case SvgRectangle rect:
                {
                    var x = rect.X.Value;
                    var y = rect.Y.Value;
                    var w = rect.Width.Value;
                    var h = rect.Height.Value;
                    sink.Add(Make(rect.ID ?? "rect",
                    [
                        [
                            new Point2(x, y), new Point2(x + w, y), new Point2(x + w, y + h),
                            new Point2(x, y + h), new Point2(x, y)
                        ]
                    ], rect.Stroke));
                    break;
                }
                case SvgCircle circle:
                    sink.Add(Make(circle.ID ?? "circle",
                        [SampleCircle(circle.CenterX.Value, circle.CenterY.Value, circle.Radius.Value, 64)],
                        circle.Stroke));
                    break;
                case SvgPath path:
                {
                    var contours = FlattenPath(path);
                    if (contours.Count > 0)
                        sink.Add(Make(path.ID ?? "path", contours, path.Stroke));
                    break;
                }
                case SvgText:
                    break;
            }
        }
    }

    private static List<Point2> FlatPoints(SvgPointCollection points)
    {
        var pts = new List<Point2>();
        for (var i = 0; i < points.Count - 1; i += 2)
            pts.Add(new Point2(points[i], points[i + 1]));
        return pts;
    }

    private static Drawable Make(string name, List<List<Point2>> contours, SvgPaintServer? stroke) =>
        new()
        {
            Name = name,
            Contours = contours,
            ColorArgb = StrokeToArgb(stroke)
        };

    private static uint? StrokeToArgb(SvgPaintServer? stroke)
    {
        if (stroke is SvgColourServer { Colour: { } c } && c.A > 0)
            return (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
        return null;
    }

    private static List<Point2> SampleCircle(double cx, double cy, double r, int n)
    {
        var pts = new List<Point2>(n + 1);
        for (var i = 0; i <= n; i++)
        {
            var a = 2 * Math.PI * i / n;
            pts.Add(new Point2(cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
        }
        return pts;
    }

    private static List<List<Point2>> FlattenPath(SvgPath path)
    {
        var contours = new List<List<Point2>>();
        var current = new List<Point2>();
        PointF cursor = default;

        if (path.PathData is null)
            return contours;

        foreach (var seg in path.PathData)
        {
            switch (seg)
            {
                case SvgMoveToSegment move:
                    if (current.Count >= 2)
                        contours.Add(current);
                    current = [];
                    cursor = move.End;
                    current.Add(new Point2(cursor.X, cursor.Y));
                    break;
                case SvgLineSegment line:
                    cursor = line.End;
                    current.Add(new Point2(cursor.X, cursor.Y));
                    break;
                case SvgCubicCurveSegment cubic:
                    SampleCubic(current, cursor, cubic.FirstControlPoint, cubic.SecondControlPoint, cubic.End, 16);
                    cursor = cubic.End;
                    break;
                case SvgQuadraticCurveSegment quad:
                    SampleQuad(current, cursor, quad.ControlPoint, quad.End, 12);
                    cursor = quad.End;
                    break;
                case SvgClosePathSegment:
                    if (current.Count > 0)
                        current.Add(current[0]);
                    break;
            }
        }

        if (current.Count >= 2)
            contours.Add(current);
        return contours;
    }

    private static void SampleCubic(List<Point2> sink, PointF p0, PointF p1, PointF p2, PointF p3, int n)
    {
        for (var i = 1; i <= n; i++)
        {
            var t = (float)i / n;
            var u = 1 - t;
            var x = u * u * u * p0.X + 3 * u * u * t * p1.X + 3 * u * t * t * p2.X + t * t * t * p3.X;
            var y = u * u * u * p0.Y + 3 * u * u * t * p1.Y + 3 * u * t * t * p2.Y + t * t * t * p3.Y;
            sink.Add(new Point2(x, y));
        }
    }

    private static void SampleQuad(List<Point2> sink, PointF p0, PointF p1, PointF p2, int n)
    {
        for (var i = 1; i <= n; i++)
        {
            var t = (float)i / n;
            var u = 1 - t;
            var x = u * u * p0.X + 2 * u * t * p1.X + t * t * p2.X;
            var y = u * u * p0.Y + 2 * u * t * p1.Y + t * t * p2.Y;
            sink.Add(new Point2(x, y));
        }
    }
}
