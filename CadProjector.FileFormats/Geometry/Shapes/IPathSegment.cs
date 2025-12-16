namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Типы сегментов пути (соответствуют SVG path commands).
    /// </summary>
    public enum PathSegmentType
    {
        /// <summary>Линейный сегмент (L, l, H, h, V, v)</summary>
        Line,
        
        /// <summary>Квадратичная кривая Безье (Q, q, T, t)</summary>
        QuadraticBezier,
        
        /// <summary>Кубическая кривая Безье (C, c, S, s)</summary>
        CubicBezier,
        
        /// <summary>Дуга эллипса (A, a)</summary>
        Arc,
        
        /// <summary>Закрытие пути (Z, z)</summary>
        Close
    }

    /// <summary>
    /// Базовый интерфейс для сегментов пути.
    /// </summary>
    public interface IPathSegment
    {
        /// <summary>
        /// Тип сегмента.
        /// </summary>
        PathSegmentType Type { get; }

        /// <summary>
        /// Конечная точка сегмента.
        /// </summary>
        Point3D EndPoint { get; }

        /// <summary>
        /// Преобразует сегмент в набор точек (тесселяция).
        /// </summary>
        /// <param name="startPoint">Начальная точка (от предыдущего сегмента).</param>
        /// <param name="tolerance">Допуск аппроксимации.</param>
        IReadOnlyList<Point3D> Tessellate(Point3D startPoint, double tolerance);

        /// <summary>
        /// Применяет трансформацию к сегменту.
        /// </summary>
        IPathSegment Transform(Matrix3x3 matrix);
    }
}

