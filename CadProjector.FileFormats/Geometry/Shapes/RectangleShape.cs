namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Прямоугольник (опционально с закруглёнными углами).
    /// Соответствует SVG &lt;rect&gt; элементу.
    /// </summary>
    public sealed class RectangleShape : IShape
    {
        public string? Id { get; set; }

        /// <summary>
        /// Координата X левого верхнего угла.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Координата Y левого верхнего угла.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Координата Z (для 3D поддержки).
        /// </summary>
        public double Z { get; }

        /// <summary>
        /// Ширина прямоугольника.
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// Высота прямоугольника.
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// Радиус скругления по X.
        /// </summary>
        public double RadiusX { get; }

        /// <summary>
        /// Радиус скругления по Y.
        /// </summary>
        public double RadiusY { get; }

        public bool IsClosed => true;

        public BoundingBox Bounds => new BoundingBox(
            new Point3D(X, Y, Z),
            new Point3D(X + Width, Y + Height, Z));

        public Point3D Center => new Point3D(X + Width / 2, Y + Height / 2, Z);

        public RectangleShape(double x, double y, double width, double height, double z = 0)
            : this(x, y, width, height, 0, 0, z)
        {
        }

        public RectangleShape(double x, double y, double width, double height, double rx, double ry, double z = 0)
        {
            X = x;
            Y = y;
            Z = z;
            Width = width;
            Height = height;
            RadiusX = Math.Min(rx, width / 2);
            RadiusY = Math.Min(ry, height / 2);
        }

        public IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0)
        {
            var result = new List<Point3D>();

            if (RadiusX > 0 && RadiusY > 0)
            {
                // Прямоугольник со скруглёнными углами
                int cornerSegments = Math.Max(2, (int)Math.Ceiling(Math.PI * Math.Max(RadiusX, RadiusY) / (2 * tolerance)));

                // Верхняя сторона (справа налево с точки зрения начала)
                result.Add(new Point3D(X + RadiusX, Y, Z));
                result.Add(new Point3D(X + Width - RadiusX, Y, Z));

                // Верхний правый угол
                AddCornerArc(result, X + Width - RadiusX, Y + RadiusY, -Math.PI / 2, 0, cornerSegments);

                // Правая сторона
                result.Add(new Point3D(X + Width, Y + RadiusY, Z));
                result.Add(new Point3D(X + Width, Y + Height - RadiusY, Z));

                // Нижний правый угол
                AddCornerArc(result, X + Width - RadiusX, Y + Height - RadiusY, 0, Math.PI / 2, cornerSegments);

                // Нижняя сторона
                result.Add(new Point3D(X + Width - RadiusX, Y + Height, Z));
                result.Add(new Point3D(X + RadiusX, Y + Height, Z));

                // Нижний левый угол
                AddCornerArc(result, X + RadiusX, Y + Height - RadiusY, Math.PI / 2, Math.PI, cornerSegments);

                // Левая сторона
                result.Add(new Point3D(X, Y + Height - RadiusY, Z));
                result.Add(new Point3D(X, Y + RadiusY, Z));

                // Верхний левый угол
                AddCornerArc(result, X + RadiusX, Y + RadiusY, Math.PI, 3 * Math.PI / 2, cornerSegments);
            }
            else
            {
                // Обычный прямоугольник
                result.Add(new Point3D(X, Y, Z));
                result.Add(new Point3D(X + Width, Y, Z));
                result.Add(new Point3D(X + Width, Y + Height, Z));
                result.Add(new Point3D(X, Y + Height, Z));
            }

            // Замыкаем контур
            result.Add(result[0]);

            return result;
        }

        private void AddCornerArc(List<Point3D> points, double cx, double cy, double startAngle, double endAngle, int segments)
        {
            for (int i = 1; i <= segments; i++)
            {
                double t = (double)i / segments;
                double angle = startAngle + (endAngle - startAngle) * t;
                points.Add(new Point3D(
                    cx + RadiusX * Math.Cos(angle),
                    cy + RadiusY * Math.Sin(angle),
                    Z));
            }
        }

        public IShape Transform(Matrix3x3 matrix)
        {
            // Для сохранения радиусов при трансформации преобразуем в PathShape
            var points = Tessellate();
            var path = new PathShape(points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                path.AddSegment(new Segments.LineSegment(points[i]));
            }
            return path.Transform(matrix);
        }

        public override string ToString() => $"Rect({X}, {Y}, {Width}x{Height})";
    }
}

