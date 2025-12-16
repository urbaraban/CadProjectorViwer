using CadProjector.FileFormats.Geometry.Shapes;

namespace CadProjector.FileFormats.Geometry.Segments
{
    /// <summary>
    /// Сегмент закрытия пути.
    /// SVG: Z или z.
    /// </summary>
    public sealed class CloseSegment : IPathSegment
    {
        public PathSegmentType Type => PathSegmentType.Close;

        /// <summary>
        /// Точка, к которой происходит закрытие (начальная точка пути).
        /// </summary>
        public Point3D EndPoint { get; }

        public CloseSegment(Point3D startPoint)
        {
            EndPoint = startPoint;
        }

        public IReadOnlyList<Point3D> Tessellate(Point3D startPoint, double tolerance)
        {
            // Если конечная точка не совпадает с начальной, добавляем линию
            if (startPoint.DistanceTo(EndPoint) > 1e-10)
            {
                return new[] { EndPoint };
            }
            return Array.Empty<Point3D>();
        }

        public IPathSegment Transform(Matrix3x3 matrix)
        {
            return new CloseSegment(matrix.TransformPoint(EndPoint));
        }

        public override string ToString() => "Z";
    }
}

