using CadProjector.Geometry.Math2D;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Devices;

/// <summary>NxM calibration mesh: control points in normalized destination space; apply via per-cell homography.</summary>
public sealed class CalibrationMesh
{
    public bool IsEnabled { get; set; }

    /// <summary>Number of cells horizontally (control points = Columns+1).</summary>
    public int Columns { get; private set; } = 1;

    /// <summary>Number of cells vertically (control points = Rows+1).</summary>
    public int Rows { get; private set; } = 1;

    /// <summary>How interior points follow corner/edge edits (legacy MorphType).</summary>
    public MeshMorphType Morph { get; set; } = MeshMorphType.Full;

    /// <summary>Selected control point for calibration-form geometry.</summary>
    public int SelectedCol { get; set; }

    /// <summary>Selected control point for calibration-form geometry.</summary>
    public int SelectedRow { get; set; }

    /// <summary>Destination points [col, row], col 0..Columns, row 0..Rows.</summary>
    public Point2[,] Points { get; private set; } = new Point2[2, 2];

    private bool _morphing;

    public Point2 CornerTL
    {
        get => Points[0, 0];
        set => Points[0, 0] = value;
    }

    public Point2 CornerTR
    {
        get => Points[Columns, 0];
        set => Points[Columns, 0] = value;
    }

    public Point2 CornerBR
    {
        get => Points[Columns, Rows];
        set => Points[Columns, Rows] = value;
    }

    public Point2 CornerBL
    {
        get => Points[0, Rows];
        set => Points[0, Rows] = value;
    }

    public CalibrationMesh() => ResetIdentity(1, 1);

    public void Resize(int columns, int rows)
    {
        columns = Math.Clamp(columns, 1, 16);
        rows = Math.Clamp(rows, 1, 16);
        var old = Points;
        var oldC = Columns;
        var oldR = Rows;
        Columns = columns;
        Rows = rows;
        Points = new Point2[columns + 1, rows + 1];
        for (var i = 0; i <= columns; i++)
        for (var j = 0; j <= rows; j++)
        {
            var u = (double)i / columns;
            var v = (double)j / rows;
            // Sample previous mesh if possible
            if (oldC > 0 && oldR > 0 && old is not null)
                Points[i, j] = SampleBilinear(old, oldC, oldR, u, v);
            else
                Points[i, j] = new Point2(u, v);
        }
    }

    public void ResetIdentity(int columns = -1, int rows = -1)
    {
        if (columns < 0) columns = Columns;
        if (rows < 0) rows = Rows;
        columns = Math.Clamp(columns, 1, 16);
        rows = Math.Clamp(rows, 1, 16);
        Columns = columns;
        Rows = rows;
        Points = new Point2[columns + 1, rows + 1];
        for (var i = 0; i <= columns; i++)
        for (var j = 0; j <= rows; j++)
            Points[i, j] = new Point2((double)i / columns, (double)j / rows);
    }

    public Point2 Apply(Point2 unit01)
    {
        if (!IsEnabled)
            return unit01;

        var u = Math.Clamp(unit01.X, 0, 1);
        var v = Math.Clamp(unit01.Y, 0, 1);
        var cx = Math.Min(Columns - 1, (int)(u * Columns));
        var cy = Math.Min(Rows - 1, (int)(v * Rows));
        var localU = u * Columns - cx;
        var localV = v * Rows - cy;

        var tl = Points[cx, cy];
        var tr = Points[cx + 1, cy];
        var br = Points[cx + 1, cy + 1];
        var bl = Points[cx, cy + 1];
        var h = Homography2D.FromUnitSquare(tl, tr, br, bl);
        return h.Transform(new Point2(localU, localV));
    }

    public IEnumerable<(Point2 A, Point2 B)> EnumerateEdges()
    {
        for (var j = 0; j <= Rows; j++)
        for (var i = 0; i < Columns; i++)
            yield return (Points[i, j], Points[i + 1, j]);

        for (var i = 0; i <= Columns; i++)
        for (var j = 0; j < Rows; j++)
            yield return (Points[i, j], Points[i, j + 1]);
    }

    public CalibrationMesh Clone()
    {
        var copy = new CalibrationMesh
        {
            IsEnabled = IsEnabled,
            Morph = Morph,
            SelectedCol = SelectedCol,
            SelectedRow = SelectedRow
        };
        copy.ResetIdentity(Columns, Rows);
        for (var i = 0; i <= Columns; i++)
        for (var j = 0; j <= Rows; j++)
            copy.Points[i, j] = Points[i, j];
        return copy;
    }

    public Point2 GetPoint(int col, int row) => Points[col, row];

    public void SetPoint(int col, int row, Point2 value) =>
        Points[Math.Clamp(col, 0, Columns), Math.Clamp(row, 0, Rows)] = value;

    public void SelectPoint(int col, int row)
    {
        SelectedCol = Math.Clamp(col, 0, Columns);
        SelectedRow = Math.Clamp(row, 0, Rows);
    }

    /// <summary>
    /// Walk control points in row-major order (legacy mesh SelectNext).
    /// Shift jumps a whole row; otherwise one point.
    /// </summary>
    public void SelectNext(bool forward, bool shift)
    {
        var cols = Columns + 1;
        var rows = Rows + 1;
        var count = cols * rows;
        if (count <= 1) return;
        var i = SelectedRow * cols + SelectedCol;
        var step = shift ? cols : 1;
        var next = ((i + (forward ? step : -step)) % count + count) % count;
        SelectPoint(next % cols, next / cols);
    }

    /// <summary>Recompute interior points from Morph mode (no-op for Single).</summary>
    public void CalculateMorph()
    {
        if (_morphing || Morph == MeshMorphType.Single)
            return;

        _morphing = true;
        try
        {
            if (Morph == MeshMorphType.Full)
                PerspectiveMorph();
            else
                GradientMorph();
        }
        finally
        {
            _morphing = false;
        }
    }

    /// <summary>Flip all Y coordinates about 0.5 (legacy MirrorMesh).</summary>
    public void Mirror()
    {
        var prev = Morph;
        Morph = MeshMorphType.Single;
        for (var i = 0; i <= Columns; i++)
        for (var j = 0; j <= Rows; j++)
        {
            var p = Points[i, j];
            Points[i, j] = new Point2(p.X, 1 - p.Y);
        }
        Morph = prev;
    }

    /// <summary>Rotate array indexing 90°; coordinates stay with their points. Swaps Columns/Rows.</summary>
    public void RotateIndex90(bool clockwise = true)
    {
        var old = Points;
        var oldC = Columns;
        var oldR = Rows;
        Columns = oldR;
        Rows = oldC;
        Points = new Point2[Columns + 1, Rows + 1];

        for (var row = 0; row <= oldR; row++)
        for (var col = 0; col <= oldC; col++)
        {
            if (clockwise)
            {
                // legacy: old[y,x] -> new[x, height-1-y] with Points[row,col]
                Points[oldR - row, col] = old[col, row];
            }
            else
            {
                // legacy: old[y,x] -> new[width-1-x, y]
                Points[row, oldC - col] = old[col, row];
            }
        }

        SelectPoint(SelectedCol, SelectedRow);
    }

    /// <summary>Rotate point coordinates 90° around (0.5, 0.5); indexing unchanged.</summary>
    public void RotateCoordinates90(bool clockwise = true)
    {
        var prev = Morph;
        Morph = MeshMorphType.Single;
        const double cx = 0.5;
        const double cy = 0.5;
        for (var i = 0; i <= Columns; i++)
        for (var j = 0; j <= Rows; j++)
        {
            var p = Points[i, j];
            var dx = p.X - cx;
            var dy = p.Y - cy;
            Points[i, j] = clockwise
                ? new Point2(cx + dy, cy - dx)
                : new Point2(cx - dy, cy + dx);
        }
        Morph = prev;
    }

    private void PerspectiveMorph()
    {
        if (Columns < 1 || Rows < 1)
            return;

        var h = Homography2D.FromUnitSquare(CornerTL, CornerTR, CornerBR, CornerBL);
        for (var i = 0; i <= Columns; i++)
        for (var j = 0; j <= Rows; j++)
        {
            if (IsCorner(i, j))
                continue;
            Points[i, j] = h.Transform(new Point2((double)i / Columns, (double)j / Rows));
        }
    }

    private void GradientMorph()
    {
        if (Morph == MeshMorphType.Vertical)
        {
            var height = Rows + 1;
            for (var col = 0; col <= Columns; col++)
            {
                var p1 = Points[col, 0];
                var p2 = Points[col, Rows];
                var delta = p2.X - p1.X;
                for (var row = 1; row < Rows; row++)
                {
                    var p = Points[col, row];
                    Points[col, row] = new Point2(p1.X + delta * (row / (double)height), p.Y);
                }
            }
        }
        else if (Morph == MeshMorphType.Horizontal)
        {
            var width = Columns + 1;
            for (var row = 0; row <= Rows; row++)
            {
                var p1 = Points[0, row];
                var p2 = Points[Columns, row];
                var delta = p2.Y - p1.Y;
                for (var col = 1; col < Columns; col++)
                {
                    var p = Points[col, row];
                    Points[col, row] = new Point2(p.X, p1.Y + delta * (col / (double)width));
                }
            }
        }
    }

    private bool IsCorner(int col, int row) =>
        (col == 0 && row == 0) ||
        (col == Columns && row == 0) ||
        (col == 0 && row == Rows) ||
        (col == Columns && row == Rows);

    private static Point2 SampleBilinear(Point2[,] grid, int cols, int rows, double u, double v)
    {
        u = Math.Clamp(u, 0, 1);
        v = Math.Clamp(v, 0, 1);
        var cx = Math.Min(cols - 1, (int)(u * cols));
        var cy = Math.Min(rows - 1, (int)(v * rows));
        var lu = u * cols - cx;
        var lv = v * rows - cy;
        var tl = grid[cx, cy];
        var tr = grid[cx + 1, cy];
        var br = grid[cx + 1, cy + 1];
        var bl = grid[cx, cy + 1];
        var top = Lerp(tl, tr, lu);
        var bot = Lerp(bl, br, lu);
        return Lerp(top, bot, lv);
    }

    private static Point2 Lerp(Point2 a, Point2 b, double t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
