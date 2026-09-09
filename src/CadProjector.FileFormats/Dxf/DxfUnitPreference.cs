using IxMilia.Dxf;

namespace CadProjector.FileFormats.Dxf;

/// <summary>User override for DXF import scale. Auto reads the file header.</summary>
public enum DxfUnitPreference
{
    Auto,
    Millimeters,
    Centimeters,
    Meters,
    Inches,
    Feet
}

public static class DxfUnitScale
{
    public static double ToMm(DxfUnitPreference preference, DxfUnits fileUnits) => preference switch
    {
        DxfUnitPreference.Millimeters => 1.0,
        DxfUnitPreference.Centimeters => 10.0,
        DxfUnitPreference.Meters => 1000.0,
        DxfUnitPreference.Inches => 25.4,
        DxfUnitPreference.Feet => 304.8,
        _ => fileUnits switch
        {
            DxfUnits.Inches => 25.4,
            DxfUnits.Feet => 304.8,
            DxfUnits.Millimeters => 1.0,
            DxfUnits.Centimeters => 10.0,
            DxfUnits.Meters => 1000.0,
            _ => 1.0
        }
    };
}
