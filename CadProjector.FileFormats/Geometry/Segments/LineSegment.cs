using CadProjector.FileFormats.Geometry.Shapes;

namespace CadProjector.FileFormats.Geometry.Segments
{
    /// <summary>
    /// Линейный сегмент пути.
    /// </summary>
    public sealed class LineSegment : IPathSegment
    {
        public PathSegmentType Type => PathSegmentType.Line;

        public Point3D EndPoint { get; }

        public LineSegment(Point3D endPoint)
        {
            EndPoint = endPoint;
        }

        public LineSegment(double x, double y, double z = 0)
            : this(new Point3D(x, y, z))
        {
        }

        public IReadOnlyList<Point3D> Tessellate(Point3D startPoint, double tolerance)
        {
            return new[] { EndPoint };
        }

        public IPathSegment Transform(Matrix3x3 matrix)
        {
            return new LineSegment(matrix.TransformPoint(EndPoint));
        }

        public override string ToString() => $"L {EndPoint}";
    }
}

