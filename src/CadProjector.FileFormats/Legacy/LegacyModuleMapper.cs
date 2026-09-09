using System.Xml.Linq;
using CadProjector.Core.Devices;

namespace CadProjector.FileFormats.Legacy;

internal static class LegacyModuleMapper
{
    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Unduplicated"] = ModuleTypes.Unduplicate,
        ["FindShortestPath"] = ModuleTypes.ShortestPath,
        ["BlankBridgeInserter"] = ModuleTypes.BlankBridge,
        ["PointMassSetter"] = ModuleTypes.PointMass,
        ["ScanRateGradientDotInserter"] = ModuleTypes.ScanRateGradient,
        ["ScanRateSplitDotInserter"] = ModuleTypes.ScanRateSplit,
        ["BlankCircleInserter"] = ModuleTypes.BlankCircle,
        ["LinesSkipper"] = ModuleTypes.LinesSkipper,
        ["LinesGroupSplitter"] = ModuleTypes.LinesGroupSplitter,
        ["LineGridSplitter"] = ModuleTypes.LineGridSplitter,
        ["AroundZero"] = ModuleTypes.AroundZero,
        ["ResolutionMultiplier"] = ModuleTypes.ResolutionMultiplier,
        ["RotateFrame2D"] = ModuleTypes.Rotate2D,
        ["Rotate2D"] = ModuleTypes.Rotate2D,
        ["MoveScale2D"] = ModuleTypes.MoveScale2D,
        ["RectProportion"] = ModuleTypes.RectProportion,
        ["ArctanCorrector"] = ModuleTypes.ArctanCorrector,
        ["AxisGradient"] = ModuleTypes.AxisGradient,
        ["ZCorrector"] = ModuleTypes.ZCorrector,
        ["DeepFrameCutter"] = ModuleTypes.DeepFrameCutter,
        ["Mesh"] = ModuleTypes.Mesh,
        ["MorphMeshCorrector"] = ModuleTypes.Mesh
    };

    public static DeviceModuleConfig? TryMap(XElement el, List<string> warnings)
    {
        var typeName = LegacyXml.TypeName(el);
        if (typeName is "ModulesGroup" or "DeviceModule" or "Modules")
            return null;

        if (!Types.TryGetValue(typeName, out var typeId))
        {
            warnings.Add($"module '{typeName}' skipped (no rewrite mapping)");
            return null;
        }

        if (typeId == ModuleTypes.Mesh)
        {
            warnings.Add("MorphMeshCorrector skipped — mesh comes from <Meshes>");
            return null;
        }

        var cfg = DeviceModuleConfig.Of(typeId, enabled: LegacyXml.Flag(el, "IsOn", true));
        foreach (var child in el.Elements())
        {
            var key = child.Name.LocalName;
            if (key is "IsOn" or "Name" or "SceneRenderModulesType" or "Category")
                continue;

            var mapped = MapParamKey(typeId, key);
            if (string.IsNullOrWhiteSpace(child.Value))
                continue;
            cfg.Set(mapped, child.Value.Trim());
        }

        return cfg;
    }

    public static void CollectModuleElements(XElement? group, List<XElement> bag)
    {
        if (group is null) return;
        var modules = group.Element("Modules")
                      ?? group.Descendants("Modules").FirstOrDefault();
        if (modules is null)
        {
            foreach (var child in group.Elements())
            {
                if (child.Name.LocalName is "ModulesGroup")
                    CollectModuleElements(child, bag);
                else if (child.Name.LocalName is not "IsOn" and not "Name" and not "IsLinear"
                         and not "IsOnlyCommonModule" and not "IsOrderModifier"
                         and not "SceneRenderModulesType")
                    bag.Add(child);
            }
            return;
        }

        foreach (var child in modules.Elements())
        {
            if (child.Name.LocalName is "ModulesGroup"
                || child.Element("Modules") is not null)
                CollectModuleElements(child, bag);
            else
                bag.Add(child);
        }
    }

    private static string MapParamKey(string typeId, string xmlName)
    {
        if (typeId == ModuleTypes.ZCorrector)
        {
            if (xmlName.Equals("X", StringComparison.OrdinalIgnoreCase)) return "CenterX";
            if (xmlName.Equals("Y", StringComparison.OrdinalIgnoreCase)) return "CenterY";
        }

        return xmlName;
    }
}
