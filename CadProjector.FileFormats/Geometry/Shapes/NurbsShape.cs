using CadProjector.FileFormats.Geometry.Segments;

namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Контрольная точка для NURBS/B-Spline кривой с весом.
    /// </summary>
    public readonly struct NurbsControlPoint
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double Weight { get; }

        public Point3D Point => new(X, Y, Z);

        public NurbsControlPoint(double x, double y, double weight = 1.0)
        {
            X = x;
            Y = y;
            Z = 0;
            Weight = weight;
        }

        public NurbsControlPoint(double x, double y, double z, double weight)
        {
            X = x;
            Y = y;
            Z = z;
            Weight = weight;
        }

        public NurbsControlPoint(Point3D point, double weight = 1.0)
        {
            X = point.X;
            Y = point.Y;
            Z = point.Z;
            Weight = weight;
        }

        public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3}) w={Weight:F3}";
    }

    /// <summary>
    /// NURBS (Non-Uniform Rational B-Spline) кривая.
    /// Поддерживает как рациональные B-сплайны (NURBS), так и обычные B-сплайны.
    /// </summary>
    public sealed class NurbsShape : IShape
    {
        private readonly List<NurbsControlPoint> _controlPoints;
        private readonly List<double> _knotVector;
        private readonly int _degree;
        private readonly bool _isRational;

        public string? Id { get; set; }

        /// <summary>
        /// Контрольные точки кривой.
        /// </summary>
        public IReadOnlyList<NurbsControlPoint> ControlPoints => _controlPoints;

        /// <summary>
        /// Узловой вектор.
        /// </summary>
        public IReadOnlyList<double> KnotVector => _knotVector;

        /// <summary>
        /// Степень кривой.
        /// </summary>
        public int Degree => _degree;

        /// <summary>
        /// Является ли кривая рациональной (NURBS) или обычным B-сплайном.
        /// </summary>
        public bool IsRational => _isRational;

        public bool IsClosed { get; set; }

        public BoundingBox Bounds
        {
            get
            {
                var points = Tessellate();
                return points.Count > 0 ? BoundingBox.FromPoints(points) : BoundingBox.Empty;
            }
        }

        public NurbsShape(
            IEnumerable<NurbsControlPoint> controlPoints,
            int degree,
            IEnumerable<double> knotVector,
            bool isRational = true)
        {
            _controlPoints = controlPoints.ToList();
            _degree = degree;
            _knotVector = knotVector.ToList();
            _isRational = isRational;
        }

        /// <summary>
        /// Создаёт NURBS из точек с единичными весами.
        /// </summary>
        public NurbsShape(
            IEnumerable<Point3D> controlPoints,
            int degree,
            IEnumerable<double> knotVector)
        {
            _controlPoints = controlPoints.Select(p => new NurbsControlPoint(p, 1.0)).ToList();
            _degree = degree;
            _knotVector = knotVector.ToList();
            _isRational = false;
        }

        public IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0)
        {
            // Оценка длины кривой для определения количества точек
            int pointCount = EstimatePointCount(tolerance);

            var points = new List<Point3D>(pointCount + 1);

            for (int i = 0; i <= pointCount; i++)
            {
                double t = (double)i / pointCount;
                points.Add(GetPointAt(t));
            }

            return points;
        }

        /// <summary>
        /// Тесселирует кривую в плотную полилинию.
        /// Возвращает PathShape из линейных сегментов без аппроксимации дугами.
        /// </summary>
        public PathShape ToOptimizedPath(double tolerance = 1.0)
        {
            double step = Math.Max(0.5, tolerance);
            var points = Tessellate(step).ToList();

            if (points.Count == 0)
                return new PathShape(Point3D.Zero);

            if (points.Count == 1)
                return new PathShape(points[0]);

            points = SimplifyPolyline(points, step * 0.25);

            var path = new PathShape(points[0]) { Id = Id };

            for (int i = 1; i < points.Count; i++)
            {
                path.AddSegment(new LineSegment(points[i]));
            }

            if (IsClosed)
            {
                path.Close();
            }

            return path;
        }

        private static List<Point3D> SimplifyPolyline(List<Point3D> points, double simplifyTolerance)
        {
            if (points.Count <= 2)
                return points;

            bool[] keep = new bool[points.Count];
            keep[0] = true;
            keep[points.Count - 1] = true;
            SimplifySection(points, 0, points.Count - 1, simplifyTolerance, keep);

            var result = new List<Point3D>();
            for (int i = 0; i < points.Count; i++)
            {
                if (keep[i])
                    result.Add(points[i]);
            }

            return result;
        }

        private static void SimplifySection(List<Point3D> points, int start, int end, double tolerance, bool[] keep)
        {
            if (end <= start + 1)
                return;

            double maxDistance = 0;
            int index = start;
            for (int i = start + 1; i < end; i++)
            {
                double distance = PerpendicularDistance(points[i], points[start], points[end]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    index = i;
                }
            }

            if (maxDistance <= tolerance)
                return;

            keep[index] = true;
            SimplifySection(points, start, index, tolerance, keep);
            SimplifySection(points, index, end, tolerance, keep);
        }

        private static double PerpendicularDistance(Point3D point, Point3D lineStart, Point3D lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared < 1e-18)
            {
                double px = point.X - lineStart.X;
                double py = point.Y - lineStart.Y;
                return Math.Sqrt(px * px + py * py);
            }

            double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared;
            t = Math.Max(0, Math.Min(1, t));
            double projX = lineStart.X + t * dx;
            double projY = lineStart.Y + t * dy;
            double distX = point.X - projX;
            double distY = point.Y - projY;
            return Math.Sqrt(distX * distX + distY * distY);
        }

        /// <summary>
        /// Вычисляет точку на кривой по параметру t [0, 1].
        /// </summary>
        public Point3D GetPointAt(double t)
        {
            if (_isRational)
                return RationalBSplinePoint(t);
            else
                return BSplinePoint(t);
        }

        #region B-Spline Algorithms

        private Point3D BSplinePoint(double t)
        {
            double x = 0, y = 0, z = 0;

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                double basis = CalculateBasisFunction(i, _degree, t);
                x += _controlPoints[i].X * basis;
                y += _controlPoints[i].Y * basis;
                z += _controlPoints[i].Z * basis;
            }

            return new Point3D(x, y, z);
        }

        private Point3D RationalBSplinePoint(double t)
        {
            double x = 0, y = 0, z = 0;
            double weightSum = 0;

            // Сначала вычисляем сумму весов
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                double basis = CalculateBasisFunction(i, _degree, t);
                weightSum += basis * _controlPoints[i].Weight;
            }

            const double eps = 1e-12;
            if (Math.Abs(weightSum) < eps)
            {
                // Fallback: возвращаем первую контрольную точку
                return _controlPoints[0].Point;
            }

            // Вычисляем взвешенную сумму
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                double basis = CalculateBasisFunction(i, _degree, t);
                double weightedBasis = basis * _controlPoints[i].Weight / weightSum;
                x += _controlPoints[i].X * weightedBasis;
                y += _controlPoints[i].Y * weightedBasis;
                z += _controlPoints[i].Z * weightedBasis;
            }

            return new Point3D(x, y, z);
        }

        /// <summary>
        /// Вычисляет базисную функцию B-сплайна (алгоритм Кокса-де Бура).
        /// </summary>
        private double CalculateBasisFunction(int i, int degree, double t)
        {
            if (_knotVector.Count == 0)
                return 0;

            // Нормализуем t к диапазону узлового вектора
            double tScaled = t * _knotVector[^1];

            int m = _knotVector.Count - 1;

            // Граничные условия
            if ((i == 0 && tScaled == _knotVector[0]) ||
                (i == m - degree - 1 && tScaled == _knotVector[m]))
                return 1.0;

            if (tScaled < _knotVector[i] || tScaled >= _knotVector[i + degree + 1])
                return 0.0;

            // Инициализация массива базисных функций
            var N = new double[degree + 1];
            for (int j = 0; j <= degree; j++)
            {
                double tRounded = Math.Round(tScaled, 6);
                N[j] = (tScaled >= _knotVector[i + j] && tRounded < _knotVector[i + j + 1]) ? 1.0 : 0.0;
            }

            // Рекурсивное вычисление
            const double eps = 1e-12;
            for (int k = 1; k <= degree; k++)
            {
                double saved;
                if (N[0] == 0)
                {
                    saved = 0;
                }
                else
                {
                    double denom = _knotVector[i + k] - _knotVector[i];
                    saved = Math.Abs(denom) < eps ? 0 : ((tScaled - _knotVector[i]) * N[0]) / denom;
                }

                for (int j = 0; j < degree - k + 1; j++)
                {
                    double uLeft = _knotVector[i + j + 1];
                    double uRight = _knotVector[i + j + k + 1];
                    double denom = uRight - uLeft;

                    if (N[j + 1] == 0 || Math.Abs(denom) < eps)
                    {
                        N[j] = saved;
                        saved = 0;
                    }
                    else
                    {
                        double temp = N[j + 1] / denom;
                        N[j] = saved + (uRight - tScaled) * temp;
                        saved = (tScaled - uLeft) * temp;
                    }
                }
            }

            return N[0];
        }

        #endregion

        #region Arc Fitting Helpers

        private int EstimatePointCount(double tolerance)
        {
            // Оценка длины кривой через выборку точек
            const int sampleCount = 30;
            double estimatedLength = 0;
            var prev = GetPointAt(0);

            for (int i = 1; i <= sampleCount; i++)
            {
                double t = (double)i / sampleCount;
                var curr = GetPointAt(t);
                estimatedLength += prev.DistanceTo(curr);
                prev = curr;
            }

            int count = (int)Math.Ceiling(estimatedLength / tolerance);
            return Math.Clamp(count, 32, 10000);
        }

        private static double CalculateMaxChord(List<Point3D> points)
        {
            if (points.Count == 0) return 0;

            double minX = points.Min(p => p.X);
            double maxX = points.Max(p => p.X);
            double minY = points.Min(p => p.Y);
            double maxY = points.Max(p => p.Y);

            return Math.Sqrt((maxX - minX) * (maxX - minX) + (maxY - minY) * (maxY - minY));
        }

        private static bool TryFitCircle(Point3D p1, Point3D p2, Point3D p3, out Point3D center, out double radius)
        {
            center = Point3D.Zero;
            radius = 0;

            // Перпендикулярные биссектрисы
            double x1 = (p2.X + p1.X) / 2;
            double y1 = (p2.Y + p1.Y) / 2;
            double dx1 = -(p2.Y - p1.Y);
            double dy1 = p2.X - p1.X;

            double x2 = (p3.X + p2.X) / 2;
            double y2 = (p3.Y + p2.Y) / 2;
            double dx2 = -(p3.Y - p2.Y);
            double dy2 = p3.X - p2.X;

            // Находим пересечение
            double denom = dy1 * dx2 - dx1 * dy2;
            if (Math.Abs(denom) < 1e-10)
                return false;

            double t = ((x1 - x2) * dy2 + (y2 - y1) * dx2) / denom;

            double cx = x1 + dx1 * t;
            double cy = y1 + dy1 * t;

            if (double.IsNaN(cx) || double.IsNaN(cy))
                return false;

            center = new Point3D(cx, cy, p1.Z);
            radius = Math.Sqrt((cx - p1.X) * (cx - p1.X) + (cy - p1.Y) * (cy - p1.Y));

            return !double.IsInfinity(radius) && !double.IsNaN(radius) && radius > 1e-6;
        }

        private static double GetAngleBetweenPoints(Point3D p1, Point3D center, Point3D p2)
        {
            double v1x = p1.X - center.X;
            double v1y = p1.Y - center.Y;
            double v2x = p2.X - center.X;
            double v2y = p2.Y - center.Y;

            double angle = Math.Atan2(v2y, v2x) - Math.Atan2(v1y, v1x);
            return angle * 180.0 / Math.PI;
        }

        private static ArcSegment? CreateArcSegment(Point3D start, Point3D middle, Point3D end)
        {
            if (!TryFitCircle(start, middle, end, out var center, out double radius))
                return null;

            double angle = GetAngleBetweenPoints(start, center, end);

            var sweepDirection = angle < 0
                ? SweepDirection.Counterclockwise
                : SweepDirection.Clockwise;

            bool isLargeArc = (Math.Abs(angle) % 360) > 180;

            return new ArcSegment(
                end,
                radius,
                radius,
                angle,
                isLargeArc,
                sweepDirection);
        }

        #endregion

        public IShape Transform(Matrix3x3 matrix)
        {
            var newPoints = _controlPoints.Select(cp =>
            {
                var transformed = matrix.TransformPoint(cp.Point);
                return new NurbsControlPoint(transformed, cp.Weight);
            });

            return new NurbsShape(newPoints, _degree, _knotVector, _isRational)
            {
                Id = Id,
                IsClosed = IsClosed
            };
        }

        public override string ToString() =>
            $"NURBS(degree={_degree}, points={_controlPoints.Count}, rational={_isRational})";
    }
}

