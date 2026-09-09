namespace CadProjector.Core.Scene;

public sealed class ProjectionScene
{
    public string Name { get; set; } = "Scene";
    public PlaneTarget Target { get; } = new();
    /// <summary>When set, drawings are projected onto this STL instead of the flat plane.</summary>
    public MeshTarget? MeshTarget { get; set; }
    public RectMask Mask { get; } = new();
    public List<Drawable> Drawables { get; } = [];
    public bool IsPlaying { get; set; }

    /// <summary>
    /// Device ids that participate in this scene. Empty means every projector in the project
    /// (legacy / single-table default).
    /// </summary>
    public List<string> BoundProjectorIds { get; set; } = [];
}
