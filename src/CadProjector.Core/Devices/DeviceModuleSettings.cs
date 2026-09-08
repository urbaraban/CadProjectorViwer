namespace CadProjector.Core.Devices;

/// <summary>Per-projector laser pipeline module settings (Z + DeepFrameCutter).</summary>
public sealed class DeviceModuleSettings
{
    public bool ZEnabled { get; set; } = true;
    public double ZDepth { get; set; } = 1;
    public double ZCenterX { get; set; } = 0.5;
    public double ZCenterY { get; set; } = 0.5;
    public bool DfcEnabled { get; set; }
    public bool DfcIgnoreHeight { get; set; }
    public double DfcDepth { get; set; } = 1;

    public void CopyFrom(DeviceModuleSettings other)
    {
        ZEnabled = other.ZEnabled;
        ZDepth = other.ZDepth;
        ZCenterX = other.ZCenterX;
        ZCenterY = other.ZCenterY;
        DfcEnabled = other.DfcEnabled;
        DfcIgnoreHeight = other.DfcIgnoreHeight;
        DfcDepth = other.DfcDepth;
    }
}
