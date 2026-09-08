namespace CadProjector.Rendering.Modules;

/// <summary>Assign corner dwell mass from turn angle (legacy PointMassSetter).</summary>
public sealed class PointMassModule : IFrameModule
{
    public string Name => "Point Mass";
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "Light mass", Min = 0, Max = 255)]
    public byte LightMass { get; set; } = 8;

    [ModuleParam(Label = "Blank mass", Min = 0, Max = 255)]
    public byte BlankMass { get; set; } = 8;

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || input.Points.Count < 2)
            return input;

        var segs = StrokeSegmentOps.ToSegments(input);
        var n = segs.Count;
        if (n == 0) return input;

        for (var i = 0; i < n; i++)
        {
            var prev = segs[(n + i - 1) % n];
            var next = segs[(i + 1) % n];
            var line = segs[i];
            var maxMass = line.IsBlank ? BlankMass : LightMass;
            if (maxMass == 0)
            {
                line.T1 = 0;
                line.T2 = 0;
                continue;
            }
            line.T1 = AngleToMass(AngleBetween(line, prev), maxMass);
            line.T2 = AngleToMass(AngleBetween(line, next), maxMass);
        }

        return StrokeSegmentOps.FromSegments(segs);
    }

    private static byte AngleToMass(double angle, byte maxMass)
    {
        var value = (int)Math.Round(maxMass * (angle / Math.PI));
        if (maxMass > 0 && value < 1) value = 1;
        if (value > maxMass) value = maxMass;
        return (byte)Math.Max(0, value);
    }

    private static double AngleBetween(StrokeSegment a, StrokeSegment b)
    {
        var ax = a.P2.X - a.P1.X;
        var ay = a.P2.Y - a.P1.Y;
        var bx = b.P2.X - b.P1.X;
        var by = b.P2.Y - b.P1.Y;
        var lena = Math.Sqrt(ax * ax + ay * ay);
        var lenb = Math.Sqrt(bx * bx + by * by);
        if (lena <= 1e-15 || lenb <= 1e-15) return 0;
        var cos = Math.Clamp((ax * bx + ay * by) / (lena * lenb), -1.0, 1.0);
        return Math.Acos(cos);
    }
}
