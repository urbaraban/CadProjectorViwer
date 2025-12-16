namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Базовый интерфейс для всех геометрических фигур.
    /// </summary>
    public interface IShape
    {
        /// <summary>
        /// Уникальный идентификатор фигуры.
        /// </summary>
        string? Id { get; set; }

        /// <summary>
        /// Ограничивающий прямоугольник фигуры.
        /// </summary>
        BoundingBox Bounds { get; }

        /// <summary>
        /// Является ли фигура замкнутой.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Преобразует фигуру в набор точек (тесселяция).
        /// </summary>
        /// <param name="tolerance">Допуск аппроксимации кривых (меньше = точнее).</param>
        /// <returns>Список точек, представляющих контур фигуры.</returns>
        IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0);

        /// <summary>
        /// Применяет трансформацию к фигуре.
        /// </summary>
        IShape Transform(Matrix3x3 matrix);
    }
}

