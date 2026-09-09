using System.Xml.Linq;
using CadProjector.Core.Devices;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.Geometry.Primitives;
using CadProjector.Logging;

namespace CadProjector.FileFormats.Legacy;

/// <summary>
/// Read-only import of legacy 2CUT XML (.2scn scene, .2cfg/.mws hub) into a JSON project.
/// Never writes the source file.
/// </summary>
public sealed class LegacyImportService
{
    private readonly DrawingImportService _drawings;

    public LegacyImportService(DrawingImportService? drawings = null)
    {
        _drawings = drawings ?? new DrawingImportService();
    }

    public static bool CanImport(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".2scn", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".2cfg", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".mws", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<LegacyImportResult> ImportAsync(
        string path,
        CancellationToken cancellationToken = default,
        IProgress<ImportProgress>? progress = null)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(path);

        progress?.Report(new ImportProgress("Reading legacy XML…", 0.1));
        var doc = XDocument.Load(path);
        var root = doc.Root ?? throw new InvalidDataException("Empty legacy file.");
        var report = new LegacyMigrationReport { SourcePath = path };
        var project = new ProjectDocument { Name = Path.GetFileNameWithoutExtension(path) };
        var baseDir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Environment.CurrentDirectory;

        var local = root.Name.LocalName;
        if (local.Equals("Moncha", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".2cfg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".mws", StringComparison.OrdinalIgnoreCase))
        {
            report.Kind = LegacyKind.Hub;
            await ImportHubAsync(root, project, report, baseDir, cancellationToken, progress);
        }
        else if (local.Equals("Objects", StringComparison.OrdinalIgnoreCase))
        {
            report.Kind = LegacyKind.ObjectsRoot;
            var scene = project.Scenes[0];
            scene.Name = Path.GetFileNameWithoutExtension(path);
            await ImportObjectsRootAsync(root, scene, report, baseDir, cancellationToken);
        }
        else if (local.Equals("Scene", StringComparison.OrdinalIgnoreCase))
        {
            report.Kind = LegacyKind.Scene;
            var scene = await ImportSceneAsync(root, report, [], baseDir, cancellationToken);
            project.Scenes = [scene];
        }
        else
            throw new InvalidDataException($"Unsupported legacy root '{local}'.");

        if (project.Scenes.Count == 0)
            project.Scenes.Add(new ProjectionScene());

        progress?.Report(new ImportProgress("Legacy import done", 1));
        CadLog.Good($"Legacy import {Path.GetFileName(path)}: {report.Imported.Count} ok, {report.Skipped.Count} skipped");
        foreach (var line in report.AllLines())
        {
            if (line.StartsWith("skip", StringComparison.Ordinal))
                CadLog.Warn(line);
            else if (line.StartsWith("warn", StringComparison.Ordinal))
                CadLog.Warn(line);
            else
                CadLog.Info(line);
        }

        return new LegacyImportResult
        {
            Project = project,
            Report = report,
            HasDevices = project.Devices.Count > 0,
            HasScenes = project.Scenes.Exists(s => s.Drawables.Count > 0 || s.Target.WidthMm > 0)
        };
    }

    private async Task ImportHubAsync(
        XElement root,
        ProjectDocument project,
        LegacyMigrationReport report,
        string baseDir,
        CancellationToken ct,
        IProgress<ImportProgress>? progress)
    {
        var outDevice = root.Element("OutputDeviceHandler") ?? root;
        var hub = outDevice.Element("HubSetting") ?? outDevice;

        var keys = hub.Element("LicenseKeys")?.Elements("Key")
            .Select(k => k.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList() ?? [];
        if (keys.Count > 0)
            report.Skip($"{keys.Count} license key(s) found — not applied (license after MVP)");

        var devicesEl = outDevice.Element("Devices") ?? root.Element("Devices");
        var devices = new List<DeviceSnapshot>();
        if (devicesEl is not null)
        {
            foreach (var el in devicesEl.Elements("Device"))
            {
                ct.ThrowIfCancellationRequested();
                devices.Add(ReadDevice(el, report));
            }
        }

        project.Devices = devices;
        if (devices.Count > 0)
            report.ImportedItem($"{devices.Count} device(s)");
        else
            report.Warn("hub has no devices");

        progress?.Report(new ImportProgress("Reading scenes…", 0.5));
        var scenesEl = hub.Element("Scenes");
        var scenes = new List<ProjectionScene>();
        if (scenesEl is not null)
        {
            foreach (var el in scenesEl.Elements("Scene"))
            {
                ct.ThrowIfCancellationRequested();
                scenes.Add(await ImportSceneAsync(el, report, devices, baseDir, ct));
            }
        }

        if (scenes.Count > 0)
            project.Scenes = scenes;
        report.ImportedItem($"{project.Scenes.Count} scene(s)");
    }

    private DeviceSnapshot ReadDevice(XElement el, LegacyMigrationReport report)
    {
        var uid = LegacyXml.Attr(el, "Uid", Guid.NewGuid().ToString("N")[..8]);
        var host = LegacyXml.Attr(el, "IP", "127.0.0.1");
        var port = (int)LegacyXml.ParseDouble(LegacyXml.Attr(el, "SendingPort", "10000"), 10000);
        var name = LegacyXml.Attr(el, "Name", "VLT");

        var size = el.Element("Size");
        TrySize(size, out var sw, out var sh, out var cx, out var cy);

        var snap = new DeviceSnapshot
        {
            Id = uid.Length > 8 && Guid.TryParse(uid, out _) ? uid : uid,
            DisplayName = name,
            Host = host,
            Port = port <= 0 ? 10000 : port,
            UseVlt = true,
            FovWidthMm = sw > 10 ? sw : 1000,
            FovHeightMm = sh > 10 ? sh : 1000,
            PoseX = sw > 10 ? cx : 500,
            PoseY = sh > 10 ? cy : 500,
            Alpha = (byte)Math.Clamp(LegacyXml.Num(el, "Alpha", 255), 0, 255),
            WidthResolution = (int)LegacyXml.Num(el, "WidthResolutuon", 65533),
            HeightResolution = (int)LegacyXml.Num(el, "HeightResolution", 65533)
        };

        var chain = new List<DeviceModuleConfig>();
        var pre = new List<XElement>();
        LegacyModuleMapper.CollectModuleElements(el.Element("PreMeshModulesGroup"), pre);
        foreach (var modEl in pre)
        {
            var mapped = LegacyModuleMapper.TryMap(modEl, report.Skipped);
            if (mapped is not null)
                chain.Add(mapped);
        }

        var meshesEl = el.Element("Meshes");
        var meshModules = new List<DeviceModuleConfig>();
        if (meshesEl is not null)
        {
            foreach (var meshEl in meshesEl.Elements("Mesh"))
            {
                var meshCfg = ReadMeshModule(meshEl, snap, report);
                if (meshCfg is not null)
                    meshModules.Add(meshCfg);
            }
        }

        if (meshModules.Count == 0)
            meshModules.Add(DeviceModuleConfig.Of(ModuleTypes.Mesh));

        chain.AddRange(meshModules);

        var post = new List<XElement>();
        LegacyModuleMapper.CollectModuleElements(el.Element("ModulesGroup"), post);
        foreach (var modEl in post)
        {
            var mapped = LegacyModuleMapper.TryMap(modEl, report.Skipped);
            if (mapped is not null)
                chain.Add(mapped);
        }

        if (chain.All(m => m.TypeId == ModuleTypes.Mesh))
        {
            var fallback = DeviceModuleConfig.DefaultChain();
            var mesh = ModuleTypes.FindMesh(fallback);
            if (mesh is not null && meshModules[0].Mesh is { } grid)
            {
                mesh.Mesh = grid;
                mesh.IsEnabled = grid.IsEnabled;
                mesh.Set("Columns", grid.Columns);
                mesh.Set("Rows", grid.Rows);
                mesh.Set("Morph", grid.Morph);
            }
            snap.ModuleChain = fallback;
        }
        else
            snap.ModuleChain = chain;

        report.ImportedItem($"device {name} {host}:{port} ({meshModules.Count} mesh, {chain.Count} modules)");
        return snap;
    }

    private static DeviceModuleConfig? ReadMeshModule(XElement meshEl, DeviceSnapshot device, LegacyMigrationReport report)
    {
        var width = (int)LegacyXml.Num(meshEl, "Width", 0);
        var height = (int)LegacyXml.Num(meshEl, "Height", 0);
        var points = meshEl.Elements("Point").ToList();
        if (width < 2 || height < 2 || points.Count < width * height)
        {
            report.Warn($"mesh '{LegacyXml.Attr(meshEl, "Name", "?")}' incomplete — skipped");
            return null;
        }

        var columns = Math.Clamp(width - 1, 1, 16);
        var rows = Math.Clamp(height - 1, 1, 16);
        var cfg = DeviceModuleConfig.Of(ModuleTypes.Mesh, true,
            ("Columns", columns), ("Rows", rows),
            ("Morph", (MeshMorphType)Math.Clamp((int)LegacyXml.Num(meshEl, "Morph", 0), 0, 3)),
            ("MiniCrossSize", LegacyXml.Num(meshEl, "MiniCrossSize", 0.02)));
        cfg.EnsureMesh();
        cfg.Mesh!.ResetIdentity(columns, rows);
        var grid = cfg.Mesh!;
        grid.IsEnabled = true;
        grid.Morph = (MeshMorphType)Math.Clamp((int)LegacyXml.Num(meshEl, "Morph", 0), 0, 3);

        var scaleX = device.FovWidthMm > 10 ? device.FovWidthMm : 1;
        var scaleY = device.FovHeightMm > 10 ? device.FovHeightMm : 1;
        var looksNormalized = points.Take(width * height).All(p =>
        {
            var x = LegacyXml.AttrNum(p, "X");
            var y = LegacyXml.AttrNum(p, "Y");
            return x is >= -0.5 and <= 1.5 && y is >= -0.5 and <= 1.5;
        });

        for (var row = 0; row <= rows; row++)
        for (var col = 0; col <= columns; col++)
        {
            var idx = row * width + col;
            if (idx >= points.Count) continue;
            var x = LegacyXml.AttrNum(points[idx], "X");
            var y = LegacyXml.AttrNum(points[idx], "Y");
            if (!looksNormalized)
            {
                x /= scaleX;
                y /= scaleY;
            }
            grid.SetPoint(col, row, new Point2(x, y));
        }

        report.ImportedItem($"mesh {LegacyXml.Attr(meshEl, "Name", "Mesh")} {width}×{height}");
        return cfg;
    }

    private async Task<ProjectionScene> ImportSceneAsync(
        XElement el,
        LegacyMigrationReport report,
        IReadOnlyList<DeviceSnapshot> devices,
        string baseDir,
        CancellationToken ct)
    {
        var scene = new ProjectionScene
        {
            Name = LegacyXml.Attr(el, "Name", "Scene")
        };

        TrySize(el.Element("Size"), out var w, out var h, out _, out _);
        if (w > 1) scene.Target.WidthMm = w;
        if (h > 1) scene.Target.HeightMm = h;

        var masks = el.Element("Masks")?.Elements().ToList() ?? [];
        if (masks.Count > 0)
        {
            if (TrySize(masks[0], out var mw, out var mh, out var mx, out var my)
                && mw > 0 && mh > 0)
            {
                scene.Mask.IsEnabled = true;
                scene.Mask.Bounds = new Rect2(mx - mw * 0.5, my - mh * 0.5, mw, mh);
                report.ImportedItem($"mask {mw:0.#}×{mh:0.#} mm");
            }
            if (masks.Count > 1)
                report.Skip($"{masks.Count - 1} extra mask(s) — MVP allows one rectangle");
        }

        var objects = el.Element("Objects");
        if (objects is not null)
        {
            foreach (var objEl in objects.Elements())
            {
                ct.ThrowIfCancellationRequested();
                var drawable = await ReadObjectAsync(objEl, report, baseDir, ct);
                if (drawable is not null)
                    scene.Drawables.Add(drawable);
            }
        }

        var bound = el.Element("Devices")?.Elements()
            .Select(x => x.Value.Trim())
            .Where(v => v.Length > 0)
            .ToList() ?? [];
        if (bound.Count > 0)
            scene.BoundProjectorIds = bound.Where(id => devices.Any(d => d.Id == id)).ToList();

        var rgb = el.Element("ProjectionSetting");
        if (rgb is not null && devices.Count > 0)
        {
            var first = devices[0];
            first.Red = (byte)Math.Clamp(LegacyXml.Num(rgb, "Red", first.Red), 0, 255);
            first.Green = (byte)Math.Clamp(LegacyXml.Num(rgb, "Green", first.Green), 0, 255);
            first.Blue = (byte)Math.Clamp(LegacyXml.Num(rgb, "Blue", first.Blue), 0, 255);
        }

        report.ImportedItem($"scene '{scene.Name}' {scene.Target.WidthMm:0.#}×{scene.Target.HeightMm:0.#} mm, {scene.Drawables.Count} object(s)");
        return scene;
    }

    private async Task ImportObjectsRootAsync(
        XElement root,
        ProjectionScene scene,
        LegacyMigrationReport report,
        string baseDir,
        CancellationToken ct)
    {
        foreach (var el in root.Elements())
        {
            ct.ThrowIfCancellationRequested();
            var path = el.Element("Path")?.Value;
            if (string.IsNullOrWhiteSpace(path))
            {
                report.Skip("Objects entry without Path");
                continue;
            }

            var resolved = ResolvePath(path, baseDir);
            var loaded = await TryLoadDrawingAsync(resolved, report, ct);
            if (loaded is null)
                continue;

            loaded.Translation = new Point3(
                LegacyXml.Num(el, "X"),
                LegacyXml.Num(el, "Y"),
                LegacyXml.Num(el, "Z"));
            scene.Drawables.Add(loaded);
            report.ImportedItem($"file {Path.GetFileName(resolved)}");
        }
    }

    private async Task<Drawable?> ReadObjectAsync(
        XElement el,
        LegacyMigrationReport report,
        string baseDir,
        CancellationToken ct)
    {
        var type = LegacyXml.Attr(el, "Type");
        Drawable? d = type switch
        {
            "1" => ReadLine(el, report),
            "file" => await ReadFileRefAsync(el, report, baseDir, ct),
            "group" => await ReadGroupAsync(el, report, baseDir, ct),
            "geometry" => ReadGeometry(el, report),
            "text" => SkipText(report),
            _ => null
        };

        if (d is null)
        {
            if (type is not "text")
                report.Skip($"object Type '{type}' skipped");
            return null;
        }

        ApplyCommon(el, d);
        ApplyTransform(el, d);
        if (el.Element("OCVTransformer") is not null)
            report.Skip($"OCVTransformer on '{d.Name}' not imported");
        return d;
    }

    private static Drawable? ReadLine(XElement el, LegacyMigrationReport report)
    {
        if (!LegacyXml.TryParsePoint(el.Element("P1")?.Value, out var p1)
            || !LegacyXml.TryParsePoint(el.Element("P2")?.Value, out var p2))
        {
            report.Skip("CadLine without P1/P2");
            return null;
        }

        return new Drawable
        {
            Name = "Line",
            Contours = [[new Point2(p1.X, p1.Y), new Point2(p2.X, p2.Y)]]
        };
    }

    private async Task<Drawable?> ReadFileRefAsync(
        XElement el,
        LegacyMigrationReport report,
        string baseDir,
        CancellationToken ct)
    {
        var path = el.Element("Path")?.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            report.Skip("file object without Path");
            return null;
        }

        var resolved = ResolvePath(path, baseDir);
        return await TryLoadDrawingAsync(resolved, report, ct);
    }

    private async Task<Drawable?> ReadGroupAsync(
        XElement el,
        LegacyMigrationReport report,
        string baseDir,
        CancellationToken ct)
    {
        var children = new List<Drawable>();
        var bag = el.Element("Children");
        if (bag is not null)
        {
            foreach (var child in bag.Elements())
            {
                var d = await ReadObjectAsync(child, report, baseDir, ct);
                if (d is not null)
                    children.Add(d);
            }
        }

        if (children.Count == 0)
            return new Drawable { Name = "Group" };
        return Drawable.CreateGroup(children, "Group");
    }

    private static Drawable? ReadGeometry(XElement el, LegacyMigrationReport report)
    {
        var data = el.Element("Geometry")?.Value;
        if (string.IsNullOrWhiteSpace(data))
        {
            report.Skip("geometry without data");
            return null;
        }

        var contours = LegacyPathParser.Parse(data);
        if (contours.Count == 0)
        {
            report.Skip("geometry parsed to empty path");
            return null;
        }

        return new Drawable { Name = "Geometry", Contours = contours };
    }

    private static Drawable? SkipText(LegacyMigrationReport report)
    {
        report.Skip("CadText ignored (CAD text out of scope)");
        return null;
    }

    private async Task<Drawable?> TryLoadDrawingAsync(string path, LegacyMigrationReport report, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            report.Skip($"file not found: {path}");
            return null;
        }

        try
        {
            var imported = await _drawings.ImportAsync(path, ct);
            if (imported.Drawables.Count == 0)
            {
                report.Skip($"empty drawing: {Path.GetFileName(path)}");
                return null;
            }

            if (imported.Drawables.Count == 1)
                return imported.Drawables[0];
            return Drawable.CreateGroup(imported.Drawables, Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            report.Skip($"failed to load {Path.GetFileName(path)}: {ex.Message}");
            return null;
        }
    }

    private static void ApplyCommon(XElement el, Drawable d)
    {
        var uid = el.Element("Uid")?.Value;
        if (Guid.TryParse(uid, out var g))
            d.Id = g;
        var name = el.Element("NameID")?.Value;
        if (!string.IsNullOrWhiteSpace(name))
            d.Name = name;
        if (el.Element("IsRender") is not null)
            d.IsVisible = LegacyXml.Flag(el, "IsRender", true);
    }

    private static void ApplyTransform(XElement el, Drawable d)
    {
        var t = el.Element("Transform");
        if (t is null)
            return;
        d.Translation = new Point3(LegacyXml.Num(t, "MX"), LegacyXml.Num(t, "MY"), LegacyXml.Num(t, "MZ"));
        d.RotationDeg = LegacyXml.Num(t, "AngleZ");
        var sx = LegacyXml.Num(t, "ScaleX", 1);
        var sy = LegacyXml.Num(t, "ScaleY", 1);
        d.Scale = Math.Abs(sx) > 1e-9 ? Math.Abs(sx) : Math.Abs(sy);
        if (d.Scale <= 0) d.Scale = 1;
    }

    private static bool TrySize(XElement? sizeEl, out double width, out double height, out double cx, out double cy)
    {
        width = height = cx = cy = 0;
        if (sizeEl is null) return false;
        if (!LegacyXml.TryParsePoint(sizeEl.Element("Point1")?.Value, out var a)
            || !LegacyXml.TryParsePoint(sizeEl.Element("Point2")?.Value, out var b))
            return false;
        var minX = Math.Min(a.X, b.X);
        var maxX = Math.Max(a.X, b.X);
        var minY = Math.Min(a.Y, b.Y);
        var maxY = Math.Max(a.Y, b.Y);
        width = maxX - minX;
        height = maxY - minY;
        cx = (minX + maxX) * 0.5;
        cy = (minY + maxY) * 0.5;
        return width > 0 && height > 0;
    }

    private static string ResolvePath(string path, string baseDir)
    {
        var trimmed = path.Trim().Trim('"');
        if (Path.IsPathRooted(trimmed))
            return trimmed;
        return Path.GetFullPath(Path.Combine(baseDir, trimmed));
    }
}
