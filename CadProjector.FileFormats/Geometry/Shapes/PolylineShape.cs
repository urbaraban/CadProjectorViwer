namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Незамкнутая ломаная линия.
    /// Соответствует SVG &lt;polyline&gt; элементу.
    /// </summary>
    public sealed class PolylineShape : IShape
    {
        private readonly List<Point3D> _points;

        public string? Id { get; set; }

        /// <summary>
        /// Вершины ломаной.
        /// </summary>
        public IReadOnlyList<Point3D> Points => _points;

        public bool IsClosed => false;

        public BoundingBox Bounds => _points.Count > 0 
            ? BoundingBox.FromPoints(_points) 
            : BoundingBox.Empty;

        public PolylineShape(IEnumerable<Point3D> points)
        {
            _points = points.ToList();
        }

        public PolylineShape(params Point3D[] points)
            : this((IEnumerable<Point3D>)points)
        {
        }

        /// <summary>
        /// Создаёт полилинию из плоского списка координат (x1, y1, x2, y2, ...).
        /// </summary>
        public static PolylineShape FromFlatCoordinates(IEnumerable<double> coordinates, double z = 0)
        {
            var points = new List<Point3D>();
            var coords = coordinates.ToArray();

            for (int i = 0; i < coords.Length - 1; i += 2)
            {
                points.Add(new Point3D(coords[i], coords[i + 1], z));
            }

            return new PolylineShape(points);
        }

        public IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0)
        {
            return _points;
        }

        public IShape Transform(Matrix3x3 matrix)
        {
            var newPoints = _points.Select(p => matrix.TransformPoint(p));
            return new PolylineShape(newPoints)
            {
                Id = Id
            };
        }

        /// <summary>
        /// Вычисляет общую длину ломаной.
        /// </summary>
        public double GetLength()
        {
            if (_points.Count < 2)
                return 0;

            double length = 0;
            for (int i = 0; i < _points.Count - 1; i++)
            {
                length += _points[i].DistanceTo(_points[i + 1]);
            }

            return length;
        }

        /// <summary>
        /// Возвращает точку на ломаной по относительному параметру t [0, 1].
        /// </summary>
        public Point3D GetPointAt(double t)
        {
            if (_points.Count == 0)
                return Point3D.Zero;
            if (_points.Count == 1)
                return _points[0];
            if (t <= 0)
                return _points[0];
            if (t >= 1)
                return _points[^1];

            double totalLength = GetLength();
            double targetLength = t * totalLength;
            double currentLength = 0;

            for (int i = 0; i < _points.Count - 1; i++)
            {
                double segmentLength = _points[i].DistanceTo(_points[i + 1]);
                if (currentLength + segmentLength >= targetLength)
                {
                    double segmentT = (targetLength - currentLength) / segmentLength;
                    return Point3D.Lerp(_points[i], _points[i + 1], segmentT);
                }
                currentLength += segmentLength;
            }

            return _points[^1];
        }

        public override string ToString() => $"Polyline({_points.Count} points)";
    }
}

