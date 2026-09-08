namespace CadProjector.FileFormats;

public sealed class DrawingImportService
{
    private readonly IReadOnlyList<IDrawingImporter> _importers;

    public DrawingImportService(IEnumerable<IDrawingImporter>? importers = null)
    {
        _importers = (importers ??
        [
            new Dxf.DxfImporter(),
            new Svg.SvgImporter()
        ]).ToList();
    }

    public IEnumerable<string> GetFileFilters()
    {
        yield return "Drawings|*.dxf;*.svg";
        yield return "DXF|*.dxf";
        yield return "SVG|*.svg";
        yield return "All|*.*";
    }

    public Task<ImportResult> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        var importer = _importers.FirstOrDefault(i => i.CanImport(path))
            ?? throw new NotSupportedException($"No importer for '{path}'.");
        return importer.ImportAsync(path, cancellationToken);
    }
}
