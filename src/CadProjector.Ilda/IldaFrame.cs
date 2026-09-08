namespace CadProjector.Ilda;

public sealed class IldaFrame
{
    public int Version { get; set; } = 5;
    public string FrameName { get; set; } = "2Cut";
    public string CompanyName { get; set; } = "2Cut";
    public List<IldaPoint> Points { get; } = [];
}
