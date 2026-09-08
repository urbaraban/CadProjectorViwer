using CadProjector.Core.Devices;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.Rendering.Modules;

namespace CadProjector.Rendering;

public sealed class DevicePipeline
{
    public LinesCollection BuildSceneFrame(ProjectionScene scene, ProjectDocument project) =>
        SceneFrameBuilder.Build(scene, project, mesh: null);

    /// <summary>Scene → FOV split → each device's module chain, in list order.</summary>
    public Dictionary<string, LinesCollection> BuildPerDevice(
        ProjectionScene scene,
        ProjectDocument project,
        IReadOnlyList<ProjectorProfile> projectors)
    {
        if (projectors.Count == 0)
            return new Dictionary<string, LinesCollection>();

        var lines = BuildSceneFrame(scene, project);
        var bags = GeometrySplitter.SplitByFov(
            lines, projectors, scene.Target.WidthMm, scene.Target.HeightMm);

        foreach (var p in projectors)
        {
            if (!bags.TryGetValue(p.Id, out var bag))
                continue;
            bags[p.Id] = ApplyDeviceStage(bag, p);
        }

        return bags;
    }

    public LinesCollection BuildFrame(ProjectionScene scene, ProjectDocument project, ProjectorProfile device)
    {
        var lines = BuildSceneFrame(scene, project);
        return ApplyDeviceStage(lines, device);
    }

    /// <summary>
    /// Runs the chain in order. A module that projects its own geometry injects it right after
    /// its own step, so every later module — the mesh included — transforms it too.
    /// </summary>
    public LinesCollection ApplyDeviceStage(LinesCollection lines, ProjectorProfile device)
    {
        foreach (var cfg in device.ModuleChain)
        {
            if (!cfg.IsEnabled)
                continue;
            if (ModuleRegistry.Materialize(cfg) is not { } module)
                continue;

            lines = module.Apply(lines) ?? lines;

            if (cfg.ProjectGeometry && module is IRenderableModule renderable)
                lines = Append(lines, renderable.GetGeometry());
        }
        return lines;
    }

    private static LinesCollection Append(LinesCollection target, LinesCollection extra)
    {
        if (extra.Points.Count == 0)
            return target;

        var merged = new LinesCollection();
        merged.Points.AddRange(target.Points);
        foreach (var p in extra.Points)
            merged.Points.Add(p);
        return merged;
    }
}
