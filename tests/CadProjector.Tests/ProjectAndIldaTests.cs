using CadProjector.Core.Devices;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.FileFormats.ProjectJson;
using CadProjector.Geometry.Primitives;
using CadProjector.Ilda;
using CadProjector.Rendering;

namespace CadProjector.Tests;

public class ProjectJsonRoundTripTests
{
    [Fact]
    public async Task SaveLoad_PreservesDrawablesMaskDevicesAndColorMode()
    {
        var project = new ProjectDocument
        {
            Name = "RoundTrip",
            ColorMode = LaserColorMode.LayerColor,
            SolidColorArgb = 0xFF00FF00
        };
        var scene = project.Scenes[0];
        scene.Name = "Main";
        scene.Target.WidthMm = 1200;
        scene.Target.HeightMm = 800;
        scene.Mask.IsEnabled = true;
        scene.Mask.Bounds = new Rect2(10, 20, 300, 400);
        scene.Drawables.Add(new Drawable
        {
            Name = "Rect",
            LayerName = "Cut",
            ColorArgb = 0xFFFF0000,
            Contours =
            [
                [new Point2(0, 0), new Point2(100, 0), new Point2(100, 50), new Point2(0, 50), new Point2(0, 0)]
            ]
        });

        var device = ProjectorProfile.CreateDefault("VLT-1", "10.0.0.5", 10001);
        device.UseVlt = true;
        device.Red = 200;
        ModuleTypes.FindMesh(device.ModuleChain)!.Mesh!.Morph = MeshMorphType.Vertical;
        ModuleTypes.FindMesh(device.ModuleChain)!.Mesh!.SetPoint(1, 1, new Point2(0.55, 0.45));
        project.Devices = [DeviceSnapshot.From(device)];

        var path = Path.Combine(Path.GetTempPath(), $"2cut-test-{Guid.NewGuid():N}.cproj");
        try
        {
            await ProjectJsonStore.SaveAsync(project, path);
            var loaded = await ProjectJsonStore.LoadAsync(path);

            Assert.Equal(ProjectDocument.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Equal("RoundTrip", loaded.Name);
            Assert.Equal(LaserColorMode.LayerColor, loaded.ColorMode);
            Assert.Equal(0xFF00FF00u, loaded.SolidColorArgb);
            Assert.Single(loaded.Scenes);
            Assert.Equal("Main", loaded.Scenes[0].Name);
            Assert.Equal(1200, loaded.Scenes[0].Target.WidthMm);
            Assert.True(loaded.Scenes[0].Mask.IsEnabled);
            Assert.Equal(10, loaded.Scenes[0].Mask.Bounds.X, 3);
            Assert.Single(loaded.Scenes[0].Drawables);
            Assert.Equal(5, loaded.Scenes[0].Drawables[0].Contours[0].Count);
            Assert.Single(loaded.Devices);
            Assert.Equal("10.0.0.5", loaded.Devices[0].Host);
            Assert.Equal(10001, loaded.Devices[0].Port);

            var meshCfg = ModuleTypes.FindMesh(loaded.Devices[0].ModuleChain);
            Assert.NotNull(meshCfg?.Mesh);
            Assert.Equal(MeshMorphType.Vertical, meshCfg!.Mesh!.Morph);
            Assert.Equal(0.55, meshCfg.Mesh.GetPoint(1, 1).X, 5);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}

public class IldaEncoderTests
{
    [Fact]
    public void ToFormat5_HeaderAndPointStride()
    {
        var frame = new IldaFrame { FrameName = "Test", CompanyName = "2Cut" };
        frame.Points.Add(new IldaPoint { X = 100, Y = -50, R = 255, G = 0, B = 0, Blanked = false });
        frame.Points.Add(new IldaPoint { X = 200, Y = 50, R = 0, G = 255, B = 0, Blanked = true });

        var bytes = IldaEncoder.ToFormat5Bytes(frame);
        Assert.Equal(32 + 2 * 8, bytes.Length);
        Assert.Equal((byte)'I', bytes[0]);
        Assert.Equal((byte)'L', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'A', bytes[3]);
        Assert.Equal(5, bytes[7]);
        Assert.Equal(0, bytes[24]); // count hi
        Assert.Equal(2, bytes[25]); // count lo
    }

    [Fact]
    public void FromNormalizedLines_MapsCenterAndBlanking()
    {
        var lines = new LinesCollection();
        lines.Points.Add(new RenderPoint { X = 0.5, Y = 0.5, Blanked = false });
        lines.Points.Add(new RenderPoint { X = 0.5, Y = 0.5, Blanked = true });

        var ilda = IldaEncoder.FromNormalizedLines(lines, 1000, 1000, 255, 0, 0);
        Assert.Equal(2, ilda.Points.Count);
        Assert.Equal(0, ilda.Points[0].X, 3);
        Assert.Equal(0, ilda.Points[0].Y, 3);
        Assert.Equal(255, ilda.Points[0].R);
        Assert.True(ilda.Points[1].Blanked);
        Assert.Equal(0, ilda.Points[1].R);
    }
}
