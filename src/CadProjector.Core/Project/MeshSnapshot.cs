using CadProjector.Core.Devices;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Project;

/// <summary>Serializable mesh snapshot for .cproj.</summary>
public sealed class MeshSnapshot
{
    public bool IsEnabled { get; set; }
    public int Columns { get; set; } = 1;
    public int Rows { get; set; } = 1;
    public int Morph { get; set; } = (int)MeshMorphType.Full;
    /// <summary>Row-major points: (Columns+1)*(Rows+1) entries of [x,y].</summary>
    public List<double[]> Points { get; set; } = [];

    public static MeshSnapshot From(CalibrationMesh mesh)
    {
        var snap = new MeshSnapshot
        {
            IsEnabled = mesh.IsEnabled,
            Columns = mesh.Columns,
            Rows = mesh.Rows,
            Morph = (int)mesh.Morph
        };
        for (var j = 0; j <= mesh.Rows; j++)
        for (var i = 0; i <= mesh.Columns; i++)
        {
            var p = mesh.GetPoint(i, j);
            snap.Points.Add([p.X, p.Y]);
        }
        return snap;
    }

    public void ApplyTo(CalibrationMesh mesh)
    {
        mesh.ResetIdentity(Math.Max(1, Columns), Math.Max(1, Rows));
        mesh.IsEnabled = IsEnabled;
        mesh.Morph = (MeshMorphType)Math.Clamp(Morph, 0, 3);
        var expected = (mesh.Columns + 1) * (mesh.Rows + 1);
        if (Points.Count < expected) return;
        var k = 0;
        for (var j = 0; j <= mesh.Rows; j++)
        for (var i = 0; i <= mesh.Columns; i++)
        {
            var xy = Points[k++];
            if (xy.Length >= 2)
                mesh.SetPoint(i, j, new Point2(xy[0], xy[1]));
        }
    }
}
