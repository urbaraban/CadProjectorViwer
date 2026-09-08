using CadProjector.Core.Devices;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering.Modules;

/// <summary>
/// Calibration warp as a chain module: everything before it is in scene space, everything
/// after it is in projector space. The control grid is owned by the module's config so a
/// device can carry several independent meshes.
/// </summary>
public sealed class CalibrationMeshModule : IRenderableModule
{
    public string Name => "Mesh";
    public bool IsEnabled { get; set; } = true;

    /// <summary>Live grid shared with the config, so table edits need no copy back.</summary>
    [ModuleParamIgnore]
    public CalibrationMesh Grid { get; set; } = CreateGrid(3, 3);

    [ModuleParam(Label = "Columns", Min = 1, Max = 16)]
    public int Columns
    {
        get => Grid.Columns;
        set
        {
            Grid.Resize(Math.Clamp(value, 1, 16), Grid.Rows);
            Grid.CalculateMorph();
        }
    }

    [ModuleParam(Label = "Rows", Min = 1, Max = 16)]
    public int Rows
    {
        get => Grid.Rows;
        set
        {
            Grid.Resize(Grid.Columns, Math.Clamp(value, 1, 16));
            Grid.CalculateMorph();
        }
    }

    /// <summary>How interior points follow edits after a control-point move.</summary>
    [ModuleParam(Label = "Morph")]
    public MeshMorphType Morph
    {
        get => Grid.Morph;
        set
        {
            if (Grid.Morph == value)
                return;
            Grid.Morph = value;
            Grid.CalculateMorph();
        }
    }

    /// <summary>Calibration guide drawn for the selected control point.</summary>
    [ModuleParam(Label = "Form")]
    public MeshCalibrationForm Form { get; set; } = MeshCalibrationForm.Rect;

    [ModuleParam(Label = "MiniCross", Min = 0, Max = 0.5, Increment = 0.005, Format = "0.###")]
    public double MiniCrossSize { get; set; } = 0.02;

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled)
            return input;

        var warped = new LinesCollection();
        foreach (var p in input.Points)
        {
            var u = Grid.Apply(new Point2(p.X, p.Y));
            warped.Points.Add(new RenderPoint
            {
                X = u.X,
                Y = u.Y,
                Z = p.Z,
                Blanked = p.Blanked,
                Mass = p.Mass,
                Color = p.Color
            });
        }
        return warped;
    }

    public LinesCollection GetGeometry()
    {
        var form = Form;
        var col = Math.Clamp(Grid.SelectedCol, 0, Grid.Columns);
        var row = Math.Clamp(Grid.SelectedRow, 0, Grid.Rows);
        return form switch
        {
            MeshCalibrationForm.Rect => BuildRect(col, row),
            MeshCalibrationForm.MiniRect => BuildMiniRect(col, row),
            MeshCalibrationForm.Cross => BuildCross(col, row),
            MeshCalibrationForm.MiniCross => BuildMiniCross(col, row),
            MeshCalibrationForm.HLine => BuildHLines(col),
            MeshCalibrationForm.WLine => BuildWLines(row),
            MeshCalibrationForm.Mesh => BuildFullMesh(),
            _ => BuildDot(col, row)
        };
    }

    public IReadOnlyList<ModuleAnchor> GetAnchors()
    {
        var anchors = new List<ModuleAnchor>((Grid.Columns + 1) * (Grid.Rows + 1));
        for (var row = 0; row <= Grid.Rows; row++)
        for (var col = 0; col <= Grid.Columns; col++)
            anchors.Add(new ModuleAnchor(AnchorIndex(col, row, Grid.Columns), Grid.GetPoint(col, row), $"{col},{row}"));
        return anchors;
    }

    public bool MoveAnchor(int index, Point2 position)
    {
        var stride = Grid.Columns + 1;
        if (index < 0 || index >= stride * (Grid.Rows + 1))
            return false;
        var col = index % stride;
        var row = index / stride;
        Grid.SetPoint(col, row, position);
        Grid.SelectPoint(col, row);
        Grid.IsEnabled = true;
        Grid.CalculateMorph();
        return true;
    }

    public void Mirror() => Grid.Mirror();

    public void RotateIndex90(bool clockwise = true)
    {
        Grid.RotateIndex90(clockwise);
        Grid.CalculateMorph();
    }

    public void RotateCoordinates90(bool clockwise = true) => Grid.RotateCoordinates90(clockwise);

    public static int AnchorIndex(int col, int row, int columns) => row * (columns + 1) + col;

    private LinesCollection BuildFullMesh()
    {
        var lines = new LinesCollection();
        foreach (var (a, b) in Grid.EnumerateEdges())
            GeometryBuilder.AddSegment(lines, a, b);
        return lines;
    }

    private LinesCollection BuildDot(int col, int row)
    {
        var lines = new LinesCollection();
        var p = Grid.GetPoint(col, row);
        GeometryBuilder.AddSegment(lines, p, p);
        return lines;
    }

    private LinesCollection BuildCross(int col, int row)
    {
        var lines = new LinesCollection();
        AppendColumn(lines, col);
        AppendRow(lines, row);
        return lines;
    }

    private LinesCollection BuildRect(int col, int row)
    {
        var lines = new LinesCollection();
        var lastCol = Grid.Columns;
        var lastRow = Grid.Rows;
        AppendColumn(lines, col);
        AppendRow(lines, lastRow - row);
        AppendColumn(lines, lastCol - col);
        AppendRow(lines, row);
        return lines;
    }

    private LinesCollection BuildMiniRect(int col, int row)
    {
        var lines = new LinesCollection();
        var backCol = col + (col < Grid.Columns ? 1 : -1);
        var backRow = row + (row < Grid.Rows ? 1 : -1);
        var a = Grid.GetPoint(col, row);
        var b = Grid.GetPoint(backCol, row);
        var c = Grid.GetPoint(backCol, backRow);
        var d = Grid.GetPoint(col, backRow);
        GeometryBuilder.AddSegment(lines, a, b);
        GeometryBuilder.AddSegment(lines, b, c);
        GeometryBuilder.AddSegment(lines, c, d);
        GeometryBuilder.AddSegment(lines, d, a);
        return lines;
    }

    private LinesCollection BuildMiniCross(int col, int row)
    {
        var lines = new LinesCollection();
        var half = Math.Max(0, MiniCrossSize);
        var c = Grid.GetPoint(col, row);
        GeometryBuilder.AddSegment(lines, new Point2(c.X - half, c.Y), new Point2(c.X + half, c.Y));
        GeometryBuilder.AddSegment(lines, new Point2(c.X, c.Y - half), new Point2(c.X, c.Y + half));
        return lines;
    }

    private LinesCollection BuildHLines(int col)
    {
        var lines = new LinesCollection();
        if (col - 1 >= 0)
            AppendColumn(lines, col - 1);
        AppendColumn(lines, col);
        if (col + 1 <= Grid.Columns)
            AppendColumn(lines, col + 1);
        return lines;
    }

    private LinesCollection BuildWLines(int row)
    {
        var lines = new LinesCollection();
        if (row - 1 >= 0)
            AppendRow(lines, row - 1);
        AppendRow(lines, row);
        if (row + 1 <= Grid.Rows)
            AppendRow(lines, row + 1);
        return lines;
    }

    private void AppendColumn(LinesCollection lines, int col)
    {
        for (var row = 1; row <= Grid.Rows; row++)
            GeometryBuilder.AddSegment(lines, Grid.GetPoint(col, row - 1), Grid.GetPoint(col, row));
    }

    private void AppendRow(LinesCollection lines, int row)
    {
        for (var col = 1; col <= Grid.Columns; col++)
            GeometryBuilder.AddSegment(lines, Grid.GetPoint(col - 1, row), Grid.GetPoint(col, row));
    }

    private static CalibrationMesh CreateGrid(int columns, int rows)
    {
        var mesh = new CalibrationMesh { IsEnabled = true, Morph = MeshMorphType.Full };
        mesh.ResetIdentity(columns, rows);
        return mesh;
    }
}
