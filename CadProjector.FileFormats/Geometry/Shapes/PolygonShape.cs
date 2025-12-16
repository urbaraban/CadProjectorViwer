namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Замкнутый многоугольник.
    /// Соответствует SVG &lt;polygon&gt; элементу.
    /// </summary>
    public sealed class PolygonShape : IShape
    {
        private readonly List<Point3D> _points;

        public string? Id { get; set; }

        /// <summary>
        /// Вершины многоугольника.
        /// </summary>
        public IReadOnlyList<Point3D> Points => _points;

        public bool IsClosed => true;

        public BoundingBox Bounds => _points.Count > 0 
            ? BoundingBox.FromPoints(_points) 
            : BoundingBox.Empty;

        public PolygonShape(IEnumerable<Point3D> points)
        {
            _points = points.ToList();
        }

        public PolygonShape(params Point3D[] points)
            : this((IEnumerable<Point3D>)points)
        {
        }

        /// <summary>
        /// Создаёт полигон из плоского списка координат (x1, y1, x2, y2, ...).
        /// </summary>
        public static PolygonShape FromFlatCoordinates(IEnumerable<double> coordinates, double z = 0)
        {
            var points = new List<Point3D>();
            var coords = coordinates.ToArray();

            for (int i = 0; i < coords.Length - 1; i += 2)
            {
                points.Add(new Point3D(coords[i], coords[i + 1], z));
            }

            return new PolygonShape(points);
        }

        public IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0)
        {
            if (_points.Count == 0)
                return Array.Empty<Point3D>();

            var result = new List<Point3D>(_points);
            // Замыкаем контур
            if (_points.Count > 0 && _points[0] != _points[^1])
            {
                result.Add(_points[0]);
            }
            return result;
        }

        public IShape Transform(Matrix3x3 matrix)
        {
            var newPoints = _points.Select(p => matrix.TransformPoint(p));
            return new PolygonShape(newPoints)
            {
                Id = Id
            };
        }

        /// <summary>
        /// Вычисляет площадь многоугольника (формула шнурования).
        /// </summary>
        public double GetArea()
        {
            if (_points.Count < 3)
                return 0;

            double area = 0;
            for (int i = 0; i < _points.Count; i++)
            {
                int j = (i + 1) % _points.Count;
                area += _points[i].X * _points[j].Y;
                area -= _points[j].X * _points[i].Y;
            }

            return Math.Abs(area) / 2;
        }

        /// <summary>
        /// Вычисляет периметр многоугольника.
        /// </summary>
        public double GetPerimeter()
        {
            if (_points.Count < 2)
                return 0;

            double perimeter = 0;
            for (int i = 0; i < _points.Count; i++)
            {
                int j = (i + 1) % _points.Count;
                perimeter += _points[i].DistanceTo(_points[j]);
            }

            return perimeter;
        }

        /// <summary>
        /// Вычисляет центроид многоугольника.
        /// </summary>
        public Point3D GetCentroid()
        {
            if (_points.Count == 0)
                return Point3D.Zero;

            double cx = 0, cy = 0, cz = 0;
            foreach (var p in _points)
            {
                cx += p.X;
                cy += p.Y;
                cz += p.Z;
            }

            return new Point3D(cx / _points.Count, cy / _points.Count, cz / _points.Count);
        }

        public override string ToString() => $"Polygon({_points.Count} points)";
    }
}

