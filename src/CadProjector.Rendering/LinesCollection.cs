namespace CadProjector.Rendering;

public sealed class LinesCollection
{
    public List<RenderPoint> Points { get; } = [];

    public int SegmentCount
    {
        get
        {
            var n = 0;
            for (var i = 1; i < Points.Count; i++)
            {
                if (!Points[i].Blanked)
                    n++;
            }
            return n;
        }
    }
}
