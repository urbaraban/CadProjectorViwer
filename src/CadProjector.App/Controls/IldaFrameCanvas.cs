using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CadProjector.Ilda;

namespace CadProjector.App.Controls;

/// <summary>
/// Draws an <see cref="IldaFrame"/> in ILDAViewer-style space:
/// coords / short.MaxValue → [-1..1], Y flipped for screen (like OpenGL ortho).
/// Blanking matches ILDAViewer: segment drawn only when both endpoints are lit
/// (unless <see cref="ShowBlanked"/>).
/// </summary>
public sealed class IldaFrameCanvas : Control
{
    public static readonly StyledProperty<IldaFrame?> FrameProperty =
        AvaloniaProperty.Register<IldaFrameCanvas, IldaFrame?>(nameof(Frame));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<IldaFrameCanvas, double>(nameof(StrokeThickness), 1.5);

    public static readonly StyledProperty<bool> ShowBlankedProperty =
        AvaloniaProperty.Register<IldaFrameCanvas, bool>(nameof(ShowBlanked));

    public static readonly StyledProperty<bool> ShowPointsProperty =
        AvaloniaProperty.Register<IldaFrameCanvas, bool>(nameof(ShowPoints));

    public IldaFrame? Frame
    {
        get => GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public bool ShowBlanked
    {
        get => GetValue(ShowBlankedProperty);
        set => SetValue(ShowBlankedProperty, value);
    }

    public bool ShowPoints
    {
        get => GetValue(ShowPointsProperty);
        set => SetValue(ShowPointsProperty, value);
    }

    static IldaFrameCanvas()
    {
        AffectsRender<IldaFrameCanvas>(
            FrameProperty, StrokeThicknessProperty, ShowBlankedProperty, ShowPointsProperty);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.Black, bounds);

        var frame = Frame;
        if (frame is null || frame.Points.Count < 2 || bounds.Width < 1 || bounds.Height < 1)
        {
            DrawCrosshair(context, bounds);
            return;
        }

        var w = bounds.Width;
        var h = bounds.Height;
        var thickness = Math.Max(0.5, StrokeThickness);
        var pts = frame.Points;
        var showBlanked = ShowBlanked;

        for (var i = 1; i < pts.Count; i++)
        {
            var p1 = pts[i - 1];
            var p2 = pts[i];
            var lit = !p1.Blanked && !p2.Blanked;
            if (!lit && !showBlanked)
                continue;

            var brush = ResolveBrush(p2, blankedSegment: !lit);
            var pen = new Pen(brush, thickness)
            {
                LineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            context.DrawLine(pen, ToScreen(p1, w, h), ToScreen(p2, w, h));
        }

        if (ShowPoints)
        {
            const double r = 2.0;
            for (var i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                if (p.Blanked && !showBlanked) continue;
                var c = ToScreen(p, w, h);
                context.DrawEllipse(
                    ResolveBrush(p, blankedSegment: p.Blanked),
                    null,
                    c, r, r);
            }
        }

        DrawCrosshair(context, bounds);
    }

    /// <summary>ILDAViewer GetVector + screen map: X/Y in ±short.MaxValue → square [-1..1], Y up.</summary>
    private static Point ToScreen(IldaPoint p, double w, double h)
    {
        var nx = p.X / short.MaxValue; // -1..1
        var ny = p.Y / short.MaxValue;
        var x = (nx * 0.5 + 0.5) * w;
        var y = (1.0 - (ny * 0.5 + 0.5)) * h; // OpenGL Y-up → Avalonia Y-down
        return new Point(x, y);
    }

    private static IBrush ResolveBrush(IldaPoint p, bool blankedSegment)
    {
        if (blankedSegment || p.Blanked)
            return new SolidColorBrush(Color.FromRgb(50, 50, 50));
        if (p.R == 0 && p.G == 0 && p.B == 0)
            return Brushes.White;
        return new SolidColorBrush(Color.FromRgb(p.R, p.G, p.B));
    }

    private static void DrawCrosshair(DrawingContext context, Rect bounds)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);
        var cx = bounds.Width * 0.5;
        var cy = bounds.Height * 0.5;
        context.DrawLine(pen, new Point(0, cy), new Point(bounds.Width, cy));
        context.DrawLine(pen, new Point(cx, 0), new Point(cx, bounds.Height));
        context.DrawRectangle(null, pen, new Rect(0.5, 0.5, bounds.Width - 1, bounds.Height - 1));
    }
}
