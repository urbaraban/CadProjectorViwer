namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Произвольный путь, состоящий из сегментов.
    /// Соответствует SVG &lt;path&gt; элементу.
    /// </summary>
    public sealed class PathShape : IPathShape
    {
        private readonly List<IPathSegment> _segments;

        public string? Id { get; set; }

        public Point3D StartPoint { get; }

        public IReadOnlyList<IPathSegment> Segments => _segments;

        public bool IsClosed => _segments.Count > 0 && 
            _segments[^1].Type == PathSegmentType.Close;

        public BoundingBox Bounds
        {
            get
            {
                var points = Tessellate();
                if (points.Count == 0)
                    return BoundingBox.Empty;
                return BoundingBox.FromPoints(points);
            }
        }

        public PathShape(Point3D startPoint)
        {
            StartPoint = startPoint;
            _segments = new List<IPathSegment>();
        }

        public PathShape(Point3D startPoint, IEnumerable<IPathSegment> segments)
        {
            StartPoint = startPoint;
            _segments = segments.ToList();
        }

        /// <summary>
        /// Добавляет сегмент к пути.
        /// </summary>
        public void AddSegment(IPathSegment segment)
        {
            _segments.Add(segment);
        }

        /// <summary>
        /// Закрывает путь.
        /// </summary>
        public void Close()
        {
            if (!IsClosed)
            {
                _segments.Add(new Segments.CloseSegment(StartPoint));
            }
        }

        public IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0)
        {
            var result = new List<Point3D> { StartPoint };
            var currentPoint = StartPoint;

            foreach (var segment in _segments)
            {
                var points = segment.Tessellate(currentPoint, tolerance);
                result.AddRange(points);

                if (points.Count > 0)
                {
                    currentPoint = points[^1];
                }
            }

            return result;
        }

        public IShape Transform(Matrix3x3 matrix)
        {
            var newStartPoint = matrix.TransformPoint(StartPoint);
            var newSegments = _segments.Select(s => s.Transform(matrix));
            return new PathShape(newStartPoint, newSegments)
            {
                Id = Id
            };
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"M {StartPoint.X},{StartPoint.Y}");
            foreach (var segment in _segments)
            {
                sb.Append(' ');
                sb.Append(segment.ToString());
            }
            return sb.ToString();
        }
    }
}

