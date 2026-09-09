using CadProjector.Logging;



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



    public Dxf.DxfUnitPreference DxfUnits { get; set; } = Dxf.DxfUnitPreference.Auto;

    public IEnumerable<string> GetFileFilters()

    {

        yield return "Drawings|*.dxf;*.svg";

        yield return "DXF|*.dxf";

        yield return "SVG|*.svg";

        yield return "All|*.*";

    }



    public async Task<ImportResult> ImportAsync(

        string path,

        CancellationToken cancellationToken = default,

        IProgress<ImportProgress>? progress = null)

    {

        var name = Path.GetFileName(path);

        var importer = _importers.FirstOrDefault(i => i.CanImport(path));

        if (importer is null)

        {

            CadLog.Error($"No importer for '{name}'");

            throw new NotSupportedException($"No importer for '{path}'.");

        }



        foreach (var importerItem in _importers)
        {
            if (importerItem is Dxf.DxfImporter dxf)
                dxf.UnitPreference = DxfUnits;
        }

        CadLog.Info($"Importing {name}…");

        progress?.Report(new ImportProgress($"Reading {name}…"));

        try

        {

            var result = await importer.ImportAsync(path, cancellationToken, progress);

            progress?.Report(new ImportProgress("Grouping…", 0.95));

            var grouped = new ImportResult

            {

                SourcePath = result.SourcePath,

                Format = result.Format,

                Drawables = CadProjector.Core.Scene.Drawable.WrapAsImportGroup(result.Drawables, name)

            };

            CadLog.Good($"Imported {grouped.Drawables.Count} group(s) from {grouped.Format}");

            return grouped;

        }

        catch (OperationCanceledException)

        {

            CadLog.Warn($"Import cancelled: {name}");

            throw;

        }

        catch (Exception ex)

        {

            CadLog.Error($"Import failed ({name}): {ex.Message}");

            throw;

        }

    }

}

