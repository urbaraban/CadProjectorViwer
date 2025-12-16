namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Эллипс.
    /// Соответствует SVG &lt;ellipse&gt; элементу.
    /// </summary>
    public sealed class EllipseShape : IShape
    {
        public string? Id { get; set; }

        /// <summary>
        /// Центр эллипса.
        /// </summary>
        public Point3D Center { get; }

        /// <summary>
        /// Радиус по оси X.
        /// </summary>
        public double RadiusX { get; }

        /// <summary>
        /// Радиус по оси Y.
        /// </summary>
        public double RadiusY { get; }

        /// <summary>
        /// Угол поворота в градусах.
        /// </summary>
        public double Rotation { get; }

        public bool IsClosed => true;

        public BoundingBox Bounds
        {
            get
            {
                // Для повёрнутого эллипса вычисляем точный bounding box
                if (Math.Abs(Rotation) < 1e-10)
                {
                    return new BoundingBox(
                        new Point3D(Center.X - RadiusX, Center.Y - RadiusY, Center.Z),
                        new Point3D(Center.X + RadiusX, Center.Y + RadiusY, Center.Z));
                }

                var points = Tessellate();
                return BoundingBox.FromPoints(points);
            }
        }

        /// <summary>
        /// Приблизительная длина периметра (формула Рамануджана).
        /// </summary>
        public double Circumference
        {
            get
            {
                double a = RadiusX;
                double b = RadiusY;
                double h = Math.Pow(a - b, 2) / Math.Pow(a + b, 2);
                return Math.PI * (a + b) * (1 + 3 * h / (10 + Math.Sqrt(4 - 3 * h)));
            }
        }

        public double Area => Math.PI * RadiusX * RadiusY;

        public EllipseShape(Point3D center, double radiusX, double radiusY, double rotation = 0)
        {
            Center = center;
            RadiusX = radiusX;
            RadiusY = radiusY;
            Rotation = rotation;
        }

        public EllipseShape(double cx, double cy, double rx, double ry, double rotation = 0, double z = 0)
            : this(new Point3D(cx, cy, z), rx, ry, rotation)
        {
        }

        public IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0)
        {
            // Количество сегментов на основе периметра и допуска
            int segments = Math.Max(8, (int)Math.Ceiling(Circumference / tolerance));

            var result = new List<Point3D>(segments + 1);
            double angleStep = 2 * Math.PI / segments;
            double rotationRad = Rotation * Math.PI / 180.0;
            double cosRot = Math.Cos(rotationRad);
            double sinRot = Math.Sin(rotationRad);

            for (int i = 0; i <= segments; i++)
            {
                double angle = i * angleStep;
                double x = RadiusX * Math.Cos(angle);
                double y = RadiusY * Math.Sin(angle);

                // Применяем поворот
                double px = Center.X + x * cosRot - y * sinRot;
                double py = Center.Y + x * sinRot + y * cosRot;

                result.Add(new Point3D(px, py, Center.Z));
            }

            return result;
        }

        public IShape Transform(Matrix3x3 matrix)
        {
            var newCenter = matrix.TransformPoint(Center);

            // Для корректной трансформации эллипса нужен более сложный алгоритм
            // Упрощённая версия: преобразуем точки и создаём path
            var points = Tessellate();
            var path = new PathShape(matrix.TransformPoint(points[0]));
            for (int i = 1; i < points.Count; i++)
            {
                path.AddSegment(new Segments.LineSegment(matrix.TransformPoint(points[i])));
            }
            path.Id = Id;
            return path;
        }

        /// <summary>
        /// Возвращает точку на эллипсе по параметрическому углу (в радианах).
        /// </summary>
        public Point3D GetPointAtAngle(double angleRadians)
        {
            double rotationRad = Rotation * Math.PI / 180.0;
            double cosRot = Math.Cos(rotationRad);
            double sinRot = Math.Sin(rotationRad);

            double x = RadiusX * Math.Cos(angleRadians);
            double y = RadiusY * Math.Sin(angleRadians);

            return new Point3D(
                Center.X + x * cosRot - y * sinRot,
                Center.Y + x * sinRot + y * cosRot,
                Center.Z);
        }

        public override string ToString() => $"Ellipse(center={Center}, rx={RadiusX}, ry={RadiusY}, rot={Rotation}°)";
    }
}

