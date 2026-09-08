using CadProjector.Core.Devices;
using CadProjector.Core.Scene;

namespace CadProjector.Core.Project;

public sealed class ProjectDocument
{
    public const int CurrentSchemaVersion = 3;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string Name { get; set; } = "Untitled";
    public LaserColorMode ColorMode { get; set; } = LaserColorMode.SolidSceneColor;
    public uint SolidColorArgb { get; set; } = 0xFFFF0000;
    public List<ProjectionScene> Scenes { get; set; } = [new()];

    /// <summary>Index of the scene currently shown in the UI / used as ActiveScene.</summary>
    public int ActiveSceneIndex { get; set; }

    /// <summary>Device cards with module chains (mesh is a module when present).</summary>
    public List<DeviceSnapshot> Devices { get; set; } = [];

    /// <summary>Schema v2 legacy: mesh of primary projector only — migrated into that device's Mesh module.</summary>
    public MeshSnapshot? CalibrationMesh { get; set; }

    public ProjectionScene ActiveScene
    {
        get
        {
            if (Scenes.Count == 0)
                throw new InvalidOperationException("Project has no scenes.");
            var i = Math.Clamp(ActiveSceneIndex, 0, Scenes.Count - 1);
            return Scenes[i];
        }
    }
}
