using CadProjector.FileFormats.Geometry.Shapes;
using CadProjector.FileFormats.Parsers;

namespace CadProjector.FileFormats
{
    /// <summary>
    /// Фабрика для создания и управления парсерами файлов.
    /// </summary>
    public class FileParserFactory
    {
        private readonly List<IFileParser> _parsers;

        /// <summary>
        /// Создаёт фабрику со стандартным набором парсеров.
        /// </summary>
        public static FileParserFactory CreateDefault()
        {
            return new FileParserFactory(new IFileParser[]
            {
                new SvgParser(),
                new DxfParser()
            });
        }

        public FileParserFactory(IEnumerable<IFileParser> parsers)
        {
            _parsers = parsers.ToList();
        }

        /// <summary>
        /// Регистрирует дополнительный парсер.
        /// </summary>
        public void RegisterParser(IFileParser parser)
        {
            if (!_parsers.Any(p => p.FormatName == parser.FormatName))
            {
                _parsers.Add(parser);
            }
        }

        /// <summary>
        /// Парсит файл и возвращает коллекцию фигур.
        /// </summary>
        public async Task<ShapeCollection> ParseFileAsync(
            string filePath,
            IProgress<ParseProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var parser = GetParser(filePath);
            if (parser == null)
            {
                var supportedFormats = string.Join(", ", GetSupportedExtensions());
                throw new NotSupportedException(
                    $"No parser found for file: {filePath}. Supported formats: {supportedFormats}");
            }

            return await parser.ParseAsync(filePath, progress, cancellationToken);
        }

        /// <summary>
        /// Возвращает парсер для указанного файла.
        /// </summary>
        public IFileParser? GetParser(string filePath)
        {
            return _parsers.FirstOrDefault(p => p.CanParse(filePath));
        }

        /// <summary>
        /// Возвращает парсер по имени формата.
        /// </summary>
        public IFileParser? GetParserByFormat(string formatName)
        {
            return _parsers.FirstOrDefault(p =>
                p.FormatName.Equals(formatName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Проверяет, поддерживается ли файл.
        /// </summary>
        public bool IsSupported(string filePath)
        {
            return _parsers.Any(p => p.CanParse(filePath));
        }

        /// <summary>
        /// Возвращает все поддерживаемые расширения.
        /// </summary>
        public string[] GetSupportedExtensions()
        {
            return _parsers
                .SelectMany(p => p.SupportedExtensions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(e => e)
                .ToArray();
        }

        /// <summary>
        /// Возвращает все поддерживаемые форматы.
        /// </summary>
        public string[] GetSupportedFormats()
        {
            return _parsers
                .Select(p => p.FormatName)
                .Distinct()
                .OrderBy(f => f)
                .ToArray();
        }

        /// <summary>
        /// Создаёт строку фильтра для диалога открытия файла.
        /// </summary>
        public string GetFileDialogFilter()
        {
            var filters = new List<string>();

            // Фильтр "Все поддерживаемые"
            var allExtensions = GetSupportedExtensions();
            filters.Add($"All Supported Files|{string.Join(";", allExtensions.Select(e => $"*{e}"))}");

            // Фильтры по форматам
            foreach (var parser in _parsers.OrderBy(p => p.FormatName))
            {
                var extensions = string.Join(";", parser.SupportedExtensions.Select(e => $"*{e}"));
                filters.Add($"{parser.FormatName} Files|{extensions}");
            }

            // Все файлы
            filters.Add("All Files|*.*");

            return string.Join("|", filters);
        }
    }
}
