using System.Globalization;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering.Modules;

/// <summary>
/// Galvo geometry correction: maps screen coordinates to mirror deflection angles
/// with an X↔Y coupling term (legacy ArctanCorrector).
/// </summary>
public sealed class ArctanCorrectorModule : PointModule, IRenderableModule
{
    private const int PreviewCells = 4;

    public override string Name => "Arctan Corrector";

    [ModuleParam(Label = "Angle X", Increment = 0.005, Format = "0.#####")]
    public double AngleX { get; set; } = 0.00001;

    [ModuleParam(Label = "Angle Y", Increment = 0.005, Format = "0.#####")]
    public double AngleY { get; set; } = 0.44;

    /// <summary>X↔Y pinch strength, ramped up toward the frame perimeter.</summary>
    [ModuleParam(Label = "Coupling scale", Increment = 0.05, Format = "0.####")]
    public double CouplingScale { get; set; } = 1;

    [ModuleParam(Label = "Coupling exponent", Increment = 0.05, Format = "0.####")]
    public double CouplingProfileExponent { get; set; } = 1;

    /// <summary>Y correction strength, peaking between center and edge.</summary>
    [ModuleParam(Label = "Y scale", Increment = 0.05, Format = "0.####")]
    public double CouplingYScale { get; set; } = 1;

    [ModuleParam(Label = "Y exponent", Increment = 0.05, Format = "0.####")]
    public double CouplingYProfileExponent { get; set; } = 1;

    /// <summary>A unit grid pushed through the correction, so the distortion is visible on the table.</summary>
    public LinesCollection GetGeometry()
    {
        var lines = new LinesCollection();
        for (var i = 0; i <= PreviewCells; i++)
        {
            var t = (double)i / PreviewCells;
            for (var j = 0; j < PreviewCells; j++)
            {
                var a = (double)j / PreviewCells;
                var b = (double)(j + 1) / PreviewCells;
                GeometryBuilder.AddSegment(lines, Warp(a, t), Warp(b, t));
                GeometryBuilder.AddSegment(lines, Warp(t, a), Warp(t, b));
            }
        }
        return lines;
    }

    /// <summary>The correction is defined by its angles alone — there is nothing to grab.</summary>
    public IReadOnlyList<ModuleAnchor> GetAnchors() => [];

    public bool MoveAnchor(int index, Point2 position) => false;

    private Point2 Warp(double x, double y)
    {
        var p = Correct(new RenderPoint { X = x, Y = y });
        return new Point2(p.X, p.Y);
    }

    protected override RenderPoint Correct(RenderPoint point)
    {
        var x = (point.X - 0.5) / 0.5;
        var y = (point.Y - 0.5) / 0.5;

        var periphery = Math.Min(1.0, Math.Max(Math.Abs(x), Math.Abs(y)));
        var ay = Math.Min(1.0, Math.Abs(y));
        var yRing = 4.0 * ay * (1.0 - ay);

        var newY = TransformY(y, AngleY, CouplingYScale, CouplingYProfileExponent, yRing);
        var newX = TransformX(x, y, AngleX, AngleY, CouplingScale, CouplingProfileExponent, periphery);

        point.X = newX * 0.5 + 0.5;
        point.Y = newY * 0.5 + 0.5;
        return point;
    }

    private static double TransformY(double y, double angle, double yScale, double yExponent, double blend)
    {
        var b = Math.Clamp(blend, 0, 1);
        var p = 1.0 + (Math.Max(yExponent, 1e-6) - 1.0) * b;
        var scale = 1.0 + (Math.Max(yScale, 1e-6) - 1.0) * b;
        var weighted = Math.Sign(y) * Math.Pow(Math.Abs(y), p);
        var a = Math.Abs(angle) < 1e-9 ? 1e-9 : angle;
        return 1.0 / a * Math.Atan(weighted * Math.Tan(a) * scale);
    }

    private static double TransformX(
        double x, double y, double angleX, double angleY,
        double couplingScale, double exponent, double blend)
    {
        var b = Math.Clamp(blend, 0, 1);
        var cosBase = Math.Cos(Math.Atan(y * Math.Tan(angleY)));
        var pinch = Math.Pow(Math.Max(cosBase, 1e-12), 1.0 + (couplingScale - 1.0) * b);
        var p = 1.0 + (Math.Max(exponent, 1e-6) - 1.0) * b;
        var weighted = Math.Sign(x) * Math.Pow(Math.Abs(x), p);
        var a = Math.Abs(angleX) < 1e-9 ? 1e-9 : angleX;
        return 1.0 / a * Math.Atan(weighted * Math.Tan(a) * pinch);
    }
}

/// <summary>
/// Piecewise-linear correction along one axis from a hand-tuned table (legacy AxisGradient).
/// The table is stored as semicolon-separated node positions, evenly spaced by default.
/// </summary>
public sealed class AxisGradientModule : PointModule, IRenderableModule
{
    private double[]? _nodes;
    private string _values = BuildDefault(11);

    public override string Name => "Axis Gradient";

    [ModuleParam(Label = "Correct Y axis")]
    public bool VerticalCorrect { get; set; } = true;

    [ModuleParam(Label = "Nodes (;)")]
    public string Values
    {
        get => _values;
        set
        {
            _values = value;
            _nodes = null;
        }
    }

    public LinesCollection GetGeometry()
    {
        var lines = new LinesCollection();
        foreach (var v in Nodes)
        {
            GeometryBuilder.AddSegment(lines,
                VerticalCorrect ? new Point2(0, v) : new Point2(v, 0),
                VerticalCorrect ? new Point2(1, v) : new Point2(v, 1));
        }
        return lines;
    }

    public IReadOnlyList<ModuleAnchor> GetAnchors()
    {
        var nodes = Nodes;
        var anchors = new ModuleAnchor[nodes.Length];
        for (var i = 0; i < nodes.Length; i++)
        {
            anchors[i] = new ModuleAnchor(i,
                VerticalCorrect ? new Point2(0.5, nodes[i]) : new Point2(nodes[i], 0.5),
                $"{i}");
        }
        return anchors;
    }

    public bool MoveAnchor(int index, Point2 position)
    {
        var nodes = Nodes;
        if (index < 0 || index >= nodes.Length)
            return false;

        var moved = (double[])nodes.Clone();
        moved[index] = VerticalCorrect ? position.Y : position.X;
        Values = string.Join(';', moved.Select(v => v.ToString("0.####", CultureInfo.InvariantCulture)));
        return true;
    }

    private double[] Nodes => _nodes ??= Parse(_values);

    protected override RenderPoint Correct(RenderPoint point)
    {
        var nodes = Nodes;
        if (nodes.Length < 2)
            return point;

        if (VerticalCorrect)
            point.Y = Sample(point.Y, nodes);
        else
            point.X = Sample(point.X, nodes);
        return point;
    }

    private static double Sample(double value, double[] nodes)
    {
        if (value < nodes[0] || value >= nodes[^1])
            return value;

        var step = 1.0 / (nodes.Length - 1);
        var index = Math.Clamp((int)Math.Ceiling(value / step), 1, nodes.Length - 1);
        var percent = (value - step * index) / step;
        var between = Math.Abs(nodes[index] - nodes[index - 1]);
        return nodes[index] + percent * between;
    }

    private static string BuildDefault(int count)
    {
        var step = 1.0 / (count - 1);
        return string.Join(';', Enumerable.Range(0, count)
            .Select(i => (i * step).ToString("0.####", CultureInfo.InvariantCulture)));
    }

    private static double[] Parse(string text) => text
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : double.NaN)
        .Where(v => !double.IsNaN(v))
        .ToArray();
}
