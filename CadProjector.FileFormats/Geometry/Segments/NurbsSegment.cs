using CadProjector.FileFormats.Geometry.Shapes;

namespace CadProjector.FileFormats.Geometry.Segments
{
    /// <summary>
    /// Тип сегмента пути для NURBS.
    /// </summary>
    public enum NurbsSegmentType
    {
        /// <summary>NURBS/Rational B-Spline</summary>
        Nurbs,
        
        /// <summary>Обычный B-Spline (без весов)</summary>
        BSpline
    }

    /// <summary>
    /// Сегмент пути, представляющий NURBS кривую.
    /// Позволяет сохранить точное математическое представление кривой.
    /// </summary>
    public sealed class NurbsSegment : IPathSegment
    {
        private readonly List<NurbsControlPoint> _controlPoints;
        private readonly List<double> _knotVector;

        public PathSegmentType Type => PathSegmentType.CubicBezier; // Для совместимости, фактически NURBS

        /// <summary>
        /// Является ли сегмент рациональным (NURBS) или обычным B-сплайном.
        /// </summary>
        public bool IsRational { get; }

        /// <summary>
        /// Степень кривой.
        /// </summary>
        public int Degree { get; }

        /// <summary>
        /// Контрольные точки.
        /// </summary>
        public IReadOnlyList<NurbsControlPoint> ControlPoints => _controlPoints;

        /// <summary>
        /// Узловой вектор.
        /// </summary>
        public IReadOnlyList<double> KnotVector => _knotVector;

        public Point3D EndPoint { get; }

        public NurbsSegment(
            IEnumerable<NurbsControlPoint> controlPoints,
            int degree,
            IEnumerable<double> knotVector,
            bool isRational = true)
        {
            _controlPoints = controlPoints.ToList();
            Degree = degree;
            _knotVector = knotVector.ToList();
            IsRational = isRational;

            // Конечная точка - последняя контрольная точка
            EndPoint = _controlPoints.Count > 0
                ? _controlPoints[^1].Point
                : Point3D.Zero;
        }

        public IReadOnlyList<Point3D> Tessellate(Point3D startPoint, double tolerance)
        {
            // Создаём временный NurbsShape для тесселяции
            var nurbs = new NurbsShape(_controlPoints, Degree, _knotVector, IsRational);
            var points = nurbs.Tessellate(tolerance).ToList();

            // Убираем начальную точку, так как она уже есть в пути
            if (points.Count > 0)
            {
                points.RemoveAt(0);
            }

            return points;
        }

        public IPathSegment Transform(Matrix3x3 matrix)
        {
            var newPoints = _controlPoints.Select(cp =>
            {
                var transformed = matrix.TransformPoint(cp.Point);
                return new NurbsControlPoint(transformed, cp.Weight);
            });

            return new NurbsSegment(newPoints, Degree, _knotVector, IsRational);
        }

        /// <summary>
        /// Преобразует сегмент в полный NurbsShape.
        /// </summary>
        public NurbsShape ToNurbsShape()
        {
            return new NurbsShape(_controlPoints, Degree, _knotVector, IsRational);
        }

        public override string ToString() =>
            $"NURBS(degree={Degree}, points={_controlPoints.Count})";
    }
}

