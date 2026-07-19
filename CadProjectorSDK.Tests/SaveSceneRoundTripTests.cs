using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Xml.Linq;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Config;
using CadProjectorSDK.Scenes;

namespace CadProjectorSDK.Tests;

public class SaveSceneRoundTripTests
{
    [Fact]
    public async Task CadLine_WriteRead_PreservesPoints()
    {
        var scene = new ProjectionScene();
        var line = new CadLine(new CadPoint3D(0, 0, 0), new CadPoint3D(10.5, 20.25, 0));
        line.Init();
        scene.Add(line);

        string path = TempScn();
        SaveScene.WriteXML(scene, path);
        var loaded = await SaveScene.LoadSceneAsync(path, null, null);

        CadLine restored = Assert.IsType<CadLine>(Assert.Single(loaded.Scene.OfType<CadLine>()));
        Assert.Equal(10.5, restored.P2.MX, 6);
        Assert.Equal(20.25, restored.P2.MY, 6);
    }

    [Fact]
    public async Task Transform_WriteRead_PreservesFields()
    {
        string fake = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"scn-file-{Guid.NewGuid():N}.dxf");
        await File.WriteAllTextAsync(fake, "0\nEND\n");

        var scene = new ProjectionScene();
        var group = new CadGroup { NameID = "File", SourcePath = fake };
        group.MX = 1.5;
        group.MY = 2.5;
        group.MZ = 0.25;
        group.ScaleX = 1.25;
        group.AngleZ = 45;
        group.Mirror = true;
        scene.Add(group);

        string path = TempScn();
        SaveScene.WriteXML(scene, path);

        var loaded = await SaveScene.LoadSceneAsync(path, null, async p =>
        {
            await Task.CompletedTask;
            return new CadGroup { SourcePath = p };
        });

        var restored = Assert.Single(loaded.Scene);
        Assert.Equal(fake, restored.SourcePath);
        Assert.Equal(1.5, restored.MX, 6);
        Assert.Equal(2.5, restored.MY, 6);
        Assert.Equal(0.25, restored.MZ, 6);
        Assert.Equal(1.25, Math.Abs(restored.ScaleX), 6);
        Assert.Equal(45, restored.AngleZ, 6);
        Assert.True(restored.Mirror);
    }

    [Fact]
    public void FileRef_Write_ContainsPathElement()
    {
        var scene = new ProjectionScene();
        scene.Add(new CadGroup { NameID = "F", SourcePath = @"C:\data\part.dxf" });

        string path = TempScn();
        SaveScene.WriteXML(scene, path);

        XDocument doc = XDocument.Load(path);
        XElement? obj = doc.Root?.Element("Objects")?.Elements("UidObject")
            .FirstOrDefault(e => (string?)e.Attribute("Type") == SceneObjectXml.TypeFile);
        Assert.NotNull(obj);
        Assert.Equal(@"C:\data\part.dxf", obj!.Element("Path")?.Value);
    }

    [Fact]
    public void InvariantCulture_Decimals_IgnoreCurrentCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            var scene = new ProjectionScene();
            var geo = new CadGeometry(new LineGeometry(new System.Windows.Point(0, 0), new System.Windows.Point(1, 1)));
            geo.MX = 1.5;
            scene.Add(geo);

            string path = TempScn();
            SaveScene.WriteXML(scene, path);
            string xml = File.ReadAllText(path);

            Assert.Contains("<MX>1.5</MX>", xml);
            Assert.DoesNotContain("<MX>1,5</MX>", xml);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task EmptyScene_WriteRead_HasSceneAndObjects()
    {
        var scene = new ProjectionScene { StepByStep = true };
        string path = TempScn();
        SaveScene.WriteXML(scene, path);

        XDocument doc = XDocument.Load(path);
        Assert.Equal("Scene", doc.Root?.Name.LocalName);
        Assert.NotNull(doc.Root?.Element("Objects"));

        var loaded = await SaveScene.LoadSceneAsync(path, null, null);
        Assert.Empty(loaded.Scene);
        Assert.True(loaded.Scene.StepByStep);
    }

    [Fact]
    public async Task OCVTransformer_Affine_WriteRead_PreservesPointsAndType()
    {
        var scene = new ProjectionScene();
        var geo = new CadGeometry(new LineGeometry(new System.Windows.Point(0, 0), new System.Windows.Point(100, 0)));
        var affine = new CadProjectorSDK.CadObjects.OCVTransforms.Affine2dTransformer();
        affine.AddProjectionPoint(new OpenCvSharp.Point2d(0, 0), new CadAnchor(10, 5, 0));
        affine.AddProjectionPoint(new OpenCvSharp.Point2d(100, 0), new CadAnchor(120, 15, 0));
        geo.OCVTransformer = affine;
        scene.Add(geo);

        string path = TempScn();
        SaveScene.WriteXML(scene, path);

        XDocument doc = XDocument.Load(path);
        XElement? ocv = doc.Root?.Element("Objects")?.Elements("UidObject")
            .Select(e => e.Element("OCVTransformer"))
            .FirstOrDefault(e => e != null);
        Assert.NotNull(ocv);
        Assert.Equal(SceneObjectXml.OcvTypeAffine, (string?)ocv!.Attribute("Type"));
        Assert.Equal(2, ocv.Elements("Point").Count());

        var loaded = await SaveScene.LoadSceneAsync(path, null, null);
        CadGeometry restored = Assert.IsType<CadGeometry>(Assert.Single(loaded.Scene));
        Assert.NotNull(restored.OCVTransformer);
        Assert.IsType<CadProjectorSDK.CadObjects.OCVTransforms.Affine2dTransformer>(restored.OCVTransformer);
        Assert.Equal(2, restored.OCVTransformer!.ProjectionPoint.Count);

        var p0 = restored.OCVTransformer.ProjectionPoint[0];
        Assert.Equal(0, p0.Item1.X, 6);
        Assert.Equal(0, p0.Item1.Y, 6);
        Assert.Equal(10, p0.Item2.X, 6);
        Assert.Equal(5, p0.Item2.Y, 6);

        var p1 = restored.OCVTransformer.ProjectionPoint[1];
        Assert.Equal(100, p1.Item1.X, 6);
        Assert.Equal(120, p1.Item2.X, 6);
        Assert.Equal(15, p1.Item2.Y, 6);
    }

    [Fact]
    public async Task OCVTransformer_Perspective_WriteRead_PreservesType()
    {
        var scene = new ProjectionScene();
        var geo = new CadGeometry(new LineGeometry(new System.Windows.Point(0, 0), new System.Windows.Point(10, 10)));
        var perspective = new CadProjectorSDK.CadObjects.OCVTransforms.PerspectiveTransformer();
        perspective.AddProjectionPoint(new OpenCvSharp.Point2d(0, 0), new CadAnchor(1, 1, 0));
        perspective.AddProjectionPoint(new OpenCvSharp.Point2d(10, 0), new CadAnchor(11, 1, 0));
        perspective.AddProjectionPoint(new OpenCvSharp.Point2d(10, 10), new CadAnchor(11, 12, 0));
        perspective.AddProjectionPoint(new OpenCvSharp.Point2d(0, 10), new CadAnchor(0, 12, 0));
        geo.OCVTransformer = perspective;
        scene.Add(geo);

        string path = TempScn();
        SaveScene.WriteXML(scene, path);
        var loaded = await SaveScene.LoadSceneAsync(path, null, null);

        CadGeometry restored = Assert.IsType<CadGeometry>(Assert.Single(loaded.Scene));
        Assert.IsType<CadProjectorSDK.CadObjects.OCVTransforms.PerspectiveTransformer>(restored.OCVTransformer);
        Assert.Equal(4, restored.OCVTransformer!.ProjectionPoint.Count);
    }

    private static string TempScn() =>
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"scene-test-{Guid.NewGuid():N}.2scn");
}
