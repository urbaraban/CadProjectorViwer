namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Интерфейс для фигур, состоящих из сегментов пути.
    /// </summary>
    public interface IPathShape : IShape
    {
        /// <summary>
        /// Начальная точка пути.
        /// </summary>
        Point3D StartPoint { get; }

        /// <summary>
        /// Сегменты, составляющие путь.
        /// </summary>
        IReadOnlyList<IPathSegment> Segments { get; }
    }
}

