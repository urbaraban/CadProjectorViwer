using CadProjector.FileFormats.Geometry.Shapes;

namespace CadProjector.FileFormats.Export
{
    /// <summary>
    /// Фабрика для создания и управления экспортёрами.
    /// </summary>
    public class ExporterFactory
    {
        private readonly List<IExporter> _exporters;

        /// <summary>
        /// Создаёт фабрику со стандартным набором экспортёров.
        /// </summary>
        public static ExporterFactory CreateDefault()
        {
            return new ExporterFactory(new IExporter[]
            {
                new SvgExporter(),
                new DxfExporter()
            });
        }

        public ExporterFactory(IEnumerable<IExporter> exporters)
        {
            _exporters = exporters.ToList();
        }

        /// <summary>
        /// Регистрирует дополнительный экспортёр.
        /// </summary>
        public void RegisterExporter(IExporter exporter)
        {
            if (!_exporters.Any(e => e.FormatName == exporter.FormatName))
            {
                _exporters.Add(exporter);
            }
        }

        /// <summary>
        /// Экспортирует коллекцию фигур в файл.
        /// </summary>
        public async Task ExportAsync(
            ShapeCollection shapes,
            string filePath,
            ExportOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var exporter = GetExporter(filePath);
            if (exporter == null)
            {
                var supportedFormats = string.Join(", ", GetSupportedExtensions());
                throw new NotSupportedException(
                    $"No exporter found for file: {filePath}. Supported formats: {supportedFormats}");
            }

            await exporter.ExportAsync(shapes, filePath, options, cancellationToken);
        }

        /// <summary>
        /// Возвращает экспортёр для указанного файла.
        /// </summary>
        public IExporter? GetExporter(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            return _exporters.FirstOrDefault(e =>
                e.SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Возвращает экспортёр по имени формата.
        /// </summary>
        public IExporter? GetExporterByFormat(string formatName)
        {
            return _exporters.FirstOrDefault(e =>
                e.FormatName.Equals(formatName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Проверяет, поддерживается ли экспорт в файл.
        /// </summary>
        public bool IsSupported(string filePath)
        {
            return GetExporter(filePath) != null;
        }

        /// <summary>
        /// Возвращает все поддерживаемые расширения.
        /// </summary>
        public string[] GetSupportedExtensions()
        {
            return _exporters
                .SelectMany(e => e.SupportedExtensions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(e => e)
                .ToArray();
        }

        /// <summary>
        /// Возвращает все поддерживаемые форматы.
        /// </summary>
        public string[] GetSupportedFormats()
        {
            return _exporters
                .Select(e => e.FormatName)
                .Distinct()
                .OrderBy(f => f)
                .ToArray();
        }

        /// <summary>
        /// Создаёт строку фильтра для диалога сохранения файла.
        /// </summary>
        public string GetFileDialogFilter()
        {
            var filters = new List<string>();

            // Фильтры по форматам
            foreach (var exporter in _exporters.OrderBy(e => e.FormatName))
            {
                var extensions = string.Join(";", exporter.SupportedExtensions.Select(e => $"*{e}"));
                filters.Add($"{exporter.FormatName} Files|{extensions}");
            }

            // Все файлы
            filters.Add("All Files|*.*");

            return string.Join("|", filters);
        }
    }
}

