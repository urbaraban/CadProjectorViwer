using CadProjector.FileFormats.Geometry.Shapes;

namespace CadProjector.FileFormats.Export
{
    /// <summary>
    /// Интерфейс для экспортёров в векторные форматы.
    /// </summary>
    public interface IExporter
    {
        /// <summary>
        /// Название формата.
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// Поддерживаемые расширения файлов.
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// Экспортирует коллекцию фигур в файл.
        /// </summary>
        /// <param name="shapes">Коллекция фигур для экспорта.</param>
        /// <param name="filePath">Путь к файлу.</param>
        /// <param name="options">Опции экспорта.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        Task ExportAsync(
            ShapeCollection shapes,
            string filePath,
            ExportOptions? options = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Опции экспорта.
    /// </summary>
    public class ExportOptions
    {
        /// <summary>
        /// Допуск для тесселяции кривых (меньше = точнее, но больше точек).
        /// </summary>
        public double TessellationTolerance { get; set; } = 1.0;

        /// <summary>
        /// Единицы измерения.
        /// </summary>
        public string Units { get; set; } = "mm";

        /// <summary>
        /// Масштаб экспорта.
        /// </summary>
        public double Scale { get; set; } = 1.0;

        /// <summary>
        /// Включить метаданные в файл.
        /// </summary>
        public bool IncludeMetadata { get; set; } = true;
    }
}

