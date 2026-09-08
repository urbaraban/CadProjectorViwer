namespace CadProjector.FileFormats;

public interface IDrawingImporter
{
    IReadOnlyList<string> Extensions { get; }
    bool CanImport(string path);
    Task<ImportResult> ImportAsync(string path, CancellationToken cancellationToken = default);
}
