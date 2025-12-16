using CadProjector.FileFormats.Geometry.Shapes;

namespace CadProjector.FileFormats.Parsers
{
    /// <summary>
    /// Интерфейс для парсеров файлов векторной графики.
    /// </summary>
    public interface IFileParser
    {
        /// <summary>
        /// Название формата (например, "SVG", "DXF").
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// Поддерживаемые расширения файлов (с точкой).
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// Проверяет, может ли парсер обработать файл.
        /// </summary>
        bool CanParse(string filePath);

        /// <summary>
        /// Парсит файл и возвращает коллекцию фигур.
        /// </summary>
        /// <param name="filePath">Путь к файлу.</param>
        /// <param name="progress">Опциональный callback для отслеживания прогресса.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        Task<ShapeCollection> ParseAsync(
            string filePath,
            IProgress<ParseProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Информация о прогрессе парсинга.
    /// </summary>
    public readonly struct ParseProgress
    {
        /// <summary>
        /// Текущий элемент (0-based).
        /// </summary>
        public int Current { get; init; }

        /// <summary>
        /// Общее количество элементов.
        /// </summary>
        public int Total { get; init; }

        /// <summary>
        /// Сообщение о текущей операции.
        /// </summary>
        public string? Message { get; init; }

        /// <summary>
        /// Прогресс в процентах (0-100).
        /// </summary>
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
    }

    /// <summary>
    /// Исключение при ошибке парсинга файла.
    /// </summary>
    public class FileParseException : Exception
    {
        public string? FilePath { get; }
        public int? LineNumber { get; }

        public FileParseException(string message) : base(message)
        {
        }

        public FileParseException(string message, string filePath) : base(message)
        {
            FilePath = filePath;
        }

        public FileParseException(string message, Exception inner) : base(message, inner)
        {
        }

        public FileParseException(string message, string filePath, Exception inner) : base(message, inner)
        {
            FilePath = filePath;
        }

        public FileParseException(string message, string filePath, int lineNumber, Exception inner) : base(message, inner)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }
}
