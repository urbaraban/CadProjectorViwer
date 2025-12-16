namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Линия (отрезок).
    /// Соответствует SVG &lt;line&gt; элементу.
    /// </summary>
    public sealed class LineShape : IShape
    {
        public string? Id { get; set; }

        /// <summary>
        /// Начальная точка линии.
        /// </summary>
        public Point3D Start { get; }

        /// <summary>
        /// Конечная точка линии.
        /// </summary>
        public Point3D End { get; }

        public bool IsClosed => false;

        public BoundingBox Bounds => BoundingBox.FromPoints(new[] { Start, End });

        public double Length => Start.DistanceTo(End);

        public LineShape(Point3D start, Point3D end)
        {
            Start = start;
            End = end;
        }

        public LineShape(double x1, double y1, double x2, double y2, double z = 0)
            : this(new Point3D(x1, y1, z), new Point3D(x2, y2, z))
        {
        }

        public IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0)
        {
            return new[] { Start, End };
        }

        public IShape Transform(Matrix3x3 matrix)
        {
            return new LineShape(
                matrix.TransformPoint(Start),
                matrix.TransformPoint(End))
            {
                Id = Id
            };
        }

        /// <summary>
        /// Возвращает точку на линии по параметру t [0, 1].
        /// </summary>
        public Point3D GetPointAt(double t)
        {
            return Point3D.Lerp(Start, End, t);
        }

        public override string ToString() => $"Line({Start} -> {End})";
    }
}

