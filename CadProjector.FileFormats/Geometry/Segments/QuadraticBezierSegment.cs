using CadProjector.FileFormats.Geometry.Shapes;

namespace CadProjector.FileFormats.Geometry.Segments
{
    /// <summary>
    /// Квадратичная кривая Безье.
    /// SVG: Q (quadratic Bezier curveto) или T (smooth quadratic curveto).
    /// </summary>
    public sealed class QuadraticBezierSegment : IPathSegment
    {
        public PathSegmentType Type => PathSegmentType.QuadraticBezier;

        /// <summary>
        /// Контрольная точка кривой.
        /// </summary>
        public Point3D ControlPoint { get; }

        public Point3D EndPoint { get; }

        public QuadraticBezierSegment(Point3D controlPoint, Point3D endPoint)
        {
            ControlPoint = controlPoint;
            EndPoint = endPoint;
        }

        public IReadOnlyList<Point3D> Tessellate(Point3D startPoint, double tolerance)
        {
            var result = new List<Point3D>();

            // Оценка длины кривой для определения количества сегментов
            double length = EstimateLength(startPoint);
            int segments = Math.Max(2, (int)Math.Ceiling(length / tolerance));

            for (int i = 1; i <= segments; i++)
            {
                double t = (double)i / segments;
                result.Add(GetPoint(startPoint, t));
            }

            return result;
        }

        /// <summary>
        /// Вычисляет точку на кривой по параметру t [0, 1].
        /// B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
        /// </summary>
        public Point3D GetPoint(Point3D startPoint, double t)
        {
            double t1 = 1 - t;
            double t1Sq = t1 * t1;
            double tSq = t * t;

            return new Point3D(
                t1Sq * startPoint.X + 2 * t1 * t * ControlPoint.X + tSq * EndPoint.X,
                t1Sq * startPoint.Y + 2 * t1 * t * ControlPoint.Y + tSq * EndPoint.Y,
                t1Sq * startPoint.Z + 2 * t1 * t * ControlPoint.Z + tSq * EndPoint.Z);
        }

        /// <summary>
        /// Оценивает длину кривой.
        /// </summary>
        private double EstimateLength(Point3D startPoint)
        {
            double length = 0;
            Point3D prev = startPoint;
            const int samples = 20;

            for (int i = 1; i <= samples; i++)
            {
                var current = GetPoint(startPoint, (double)i / samples);
                length += prev.DistanceTo(current);
                prev = current;
            }

            return length;
        }

        public IPathSegment Transform(Matrix3x3 matrix)
        {
            return new QuadraticBezierSegment(
                matrix.TransformPoint(ControlPoint),
                matrix.TransformPoint(EndPoint));
        }

        public override string ToString() => $"Q {ControlPoint} {EndPoint}";
    }
}

