using CadProjector.Core.Scene;

namespace CadProjector.FileFormats;

public sealed class ImportResult
{
    public string SourcePath { get; init; } = "";
    public string Format { get; init; } = "";
    public List<Drawable> Drawables { get; init; } = [];
}
