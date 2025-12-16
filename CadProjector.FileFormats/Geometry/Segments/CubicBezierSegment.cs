using CadProjector.FileFormats.Geometry.Shapes;

namespace CadProjector.FileFormats.Geometry.Segments
{
    /// <summary>
    /// Кубическая кривая Безье.
    /// SVG: C (curveto) или S (smooth curveto).
    /// </summary>
    public sealed class CubicBezierSegment : IPathSegment
    {
        public PathSegmentType Type => PathSegmentType.CubicBezier;

        /// <summary>
        /// Первая контрольная точка.
        /// </summary>
        public Point3D ControlPoint1 { get; }

        /// <summary>
        /// Вторая контрольная точка.
        /// </summary>
        public Point3D ControlPoint2 { get; }

        public Point3D EndPoint { get; }

        public CubicBezierSegment(Point3D controlPoint1, Point3D controlPoint2, Point3D endPoint)
        {
            ControlPoint1 = controlPoint1;
            ControlPoint2 = controlPoint2;
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
        /// B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃
        /// </summary>
        public Point3D GetPoint(Point3D startPoint, double t)
        {
            double t1 = 1 - t;
            double t1Sq = t1 * t1;
            double t1Cu = t1Sq * t1;
            double tSq = t * t;
            double tCu = tSq * t;

            return new Point3D(
                t1Cu * startPoint.X + 3 * t1Sq * t * ControlPoint1.X + 3 * t1 * tSq * ControlPoint2.X + tCu * EndPoint.X,
                t1Cu * startPoint.Y + 3 * t1Sq * t * ControlPoint1.Y + 3 * t1 * tSq * ControlPoint2.Y + tCu * EndPoint.Y,
                t1Cu * startPoint.Z + 3 * t1Sq * t * ControlPoint1.Z + 3 * t1 * tSq * ControlPoint2.Z + tCu * EndPoint.Z);
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
            return new CubicBezierSegment(
                matrix.TransformPoint(ControlPoint1),
                matrix.TransformPoint(ControlPoint2),
                matrix.TransformPoint(EndPoint));
        }

        public override string ToString() => $"C {ControlPoint1} {ControlPoint2} {EndPoint}";
    }
}

