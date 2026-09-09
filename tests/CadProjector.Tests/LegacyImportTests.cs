using CadProjector.Core.Devices;
using CadProjector.FileFormats.Legacy;

namespace CadProjector.Tests;

public class LegacyImportTests
{
    [Fact]
    public async Task Hub_ImportsDeviceMeshSceneLineAndMask()
    {
        var path = WriteTemp(".2cfg", HubXml);
        try
        {
            var result = await new LegacyImportService().ImportAsync(path);
            Assert.Equal(LegacyKind.Hub, result.Report.Kind);
            Assert.Single(result.Project.Devices);
            var device = result.Project.Devices[0];
            Assert.Equal("10.0.0.5", device.Host);
            Assert.Equal(10001, device.Port);
            Assert.Equal("VLT-Shop", device.DisplayName);

            var mesh = ModuleTypes.FindMesh(device.ModuleChain);
            Assert.NotNull(mesh?.Mesh);
            Assert.Equal(1, mesh!.Mesh!.Columns);
            Assert.Equal(1, mesh.Mesh.Rows);
            Assert.Equal(0, mesh.Mesh.GetPoint(0, 0).X, 5);
            Assert.Equal(1, mesh.Mesh.GetPoint(1, 0).X, 5);

            Assert.Contains(device.ModuleChain, m => m.TypeId == ModuleTypes.Unduplicate);
            var z = device.ModuleChain.First(m => m.TypeId == ModuleTypes.ZCorrector);
            Assert.Equal("0.4", z.Get("CenterX"));
            Assert.Equal("0.6", z.Get("CenterY"));
            Assert.Equal("1.2", z.Get("Depth"));

            Assert.Single(result.Project.Scenes);
            var scene = result.Project.Scenes[0];
            Assert.Equal(2000, scene.Target.WidthMm, 3);
            Assert.Equal(1500, scene.Target.HeightMm, 3);
            Assert.True(scene.Mask.IsEnabled);
            Assert.Single(scene.Drawables);
            Assert.Equal("Edge", scene.Drawables[0].Name);
            Assert.Equal(10, scene.Drawables[0].Translation.X, 3);
            Assert.Equal(15, scene.Drawables[0].RotationDeg, 3);
            Assert.Equal(2, scene.Drawables[0].Contours[0].Count);

            Assert.Contains(result.Report.Skipped, s => s.Contains("license", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Scene_ImportsEmbeddedPathGeometry()
    {
        var path = WriteTemp(".2scn", SceneXml);
        try
        {
            var result = await new LegacyImportService().ImportAsync(path);
            Assert.Equal(LegacyKind.Scene, result.Report.Kind);
            var d = Assert.Single(result.Project.Scenes[0].Drawables);
            Assert.Equal("Rect", d.Name);
            Assert.True(d.Contours[0].Count >= 4);
            Assert.Equal(0, d.Contours[0][0].X, 5);
            Assert.Equal(100, d.Contours[0][1].X, 5);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ObjectsRoot_SkipsMissingFile()
    {
        var path = WriteTemp(".2scn", """
            <Objects>
              <Item>
                <Path>C:\missing\no-such.dxf</Path>
                <X>1</X><Y>2</Y><Z>3</Z>
              </Item>
            </Objects>
            """);
        try
        {
            var result = await new LegacyImportService().ImportAsync(path);
            Assert.Equal(LegacyKind.ObjectsRoot, result.Report.Kind);
            Assert.Empty(result.Project.Scenes[0].Drawables);
            Assert.Contains(result.Report.Skipped, s => s.Contains("not found", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void PathParser_ReadsMovetoLinetoClose()
    {
        var contours = LegacyPathParser.Parse("M 0,0 L 10,0 10,5 Z");
        var c = Assert.Single(contours);
        Assert.True(c.Count >= 4);
        Assert.Equal(0, c[0].X, 5);
        Assert.Equal(10, c[1].X, 5);
    }

    private static string WriteTemp(string ext, string xml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"2cut-legacy-{Guid.NewGuid():N}{ext}");
        File.WriteAllText(path, xml);
        return path;
    }

    private const string HubXml = """
        <Moncha>
          <OutputDeviceHandler>
            <HubSetting>
              <LicenseKeys>
                <Key>TESTKEY</Key>
              </LicenseKeys>
              <Scenes>
                <Scene Name="Table" ID="0" Uid="11111111-1111-1111-1111-111111111111">
                  <Size>
                    <Point1>0;0;0</Point1>
                    <Point2>2000;1500;0</Point2>
                  </Size>
                  <Objects>
                    <UidObject Type="1">
                      <P1>0;0;0</P1>
                      <P2>100;50;0</P2>
                      <NameID>Edge</NameID>
                      <IsRender>True</IsRender>
                      <Transform>
                        <MX>10</MX><MY>20</MY><MZ>5</MZ>
                        <ScaleX>1</ScaleX><ScaleY>1</ScaleY><ScaleZ>1</ScaleZ>
                        <AngleX>0</AngleX><AngleY>0</AngleY><AngleZ>15</AngleZ>
                        <Mirror>False</Mirror><MirrorX>False</MirrorX>
                        <CenterX>0</CenterX><CenterY>0</CenterY>
                      </Transform>
                    </UidObject>
                  </Objects>
                  <Masks>
                    <Mask>
                      <Point1>100;100;0</Point1>
                      <Point2>500;400;0</Point2>
                    </Mask>
                  </Masks>
                  <Devices>
                    <Uid>22222222-2222-2222-2222-222222222222</Uid>
                  </Devices>
                </Scene>
              </Scenes>
            </HubSetting>
            <Devices>
              <Device Name="VLT-Shop" Type="0" IP="10.0.0.5" SendingPort="10001" Uid="22222222-2222-2222-2222-222222222222">
                <Size>
                  <Point1>0;0;0</Point1>
                  <Point2>2000;1500;0</Point2>
                </Size>
                <Meshes>
                  <Mesh Name="Main" Uid="33333333-3333-3333-3333-333333333333">
                    <Morph>0</Morph>
                    <Width>2</Width>
                    <Height>2</Height>
                    <Point X="0" Y="0" Z="0" />
                    <Point X="1" Y="0" Z="0" />
                    <Point X="0" Y="1" Z="0" />
                    <Point X="1" Y="1" Z="0" />
                  </Mesh>
                </Meshes>
                <PreMeshModulesGroup>
                  <ModulesGroup>
                    <Modules>
                      <DeviceModule xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:type="Unduplicated">
                        <IsOn>true</IsOn>
                      </DeviceModule>
                    </Modules>
                  </ModulesGroup>
                </PreMeshModulesGroup>
                <ModulesGroup>
                  <ModulesGroup>
                    <Modules>
                      <DeviceModule xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:type="ZCorrector">
                        <IsOn>true</IsOn>
                        <Depth>1.2</Depth>
                        <X>0.4</X>
                        <Y>0.6</Y>
                      </DeviceModule>
                    </Modules>
                  </ModulesGroup>
                </ModulesGroup>
              </Device>
            </Devices>
          </OutputDeviceHandler>
        </Moncha>
        """;

    private const string SceneXml = """
        <Scene Name="Cut" ID="0" Uid="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa">
          <Size>
            <Point1>0;0;0</Point1>
            <Point2>1000;800;0</Point2>
          </Size>
          <Objects>
            <UidObject Type="geometry">
              <NameID>Rect</NameID>
              <Geometry>M 0,0 L 100,0 100,50 0,50 Z</Geometry>
            </UidObject>
            <UidObject Type="text">
              <Text>hello</Text>
            </UidObject>
          </Objects>
        </Scene>
        """;
}
