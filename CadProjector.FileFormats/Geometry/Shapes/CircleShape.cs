namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Окружность.
    /// Соответствует SVG &lt;circle&gt; элементу.
    /// </summary>
    public sealed class CircleShape : IShape
    {
        public string? Id { get; set; }

        /// <summary>
        /// Центр окружности.
        /// </summary>
        public Point3D Center { get; }

        /// <summary>
        /// Радиус окружности.
        /// </summary>
        public double Radius { get; }

        public bool IsClosed => true;

        public BoundingBox Bounds => new BoundingBox(
            new Point3D(Center.X - Radius, Center.Y - Radius, Center.Z),
            new Point3D(Center.X + Radius, Center.Y + Radius, Center.Z));

        public double Circumference => 2 * Math.PI * Radius;

        public double Area => Math.PI * Radius * Radius;

        public CircleShape(Point3D center, double radius)
        {
            Center = center;
            Radius = radius;
        }

        public CircleShape(double cx, double cy, double radius, double z = 0)
            : this(new Point3D(cx, cy, z), radius)
        {
        }

        public IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0)
        {
            // Количество сегментов на основе радиуса и допуска
            int segments = Math.Max(8, (int)Math.Ceiling(Circumference / tolerance));

            var result = new List<Point3D>(segments + 1);
            double angleStep = 2 * Math.PI / segments;

            for (int i = 0; i <= segments; i++)
            {
                double angle = i * angleStep;
                result.Add(new Point3D(
                    Center.X + Radius * Math.Cos(angle),
                    Center.Y + Radius * Math.Sin(angle),
                    Center.Z));
            }

            return result;
        }

        public IShape Transform(Matrix3x3 matrix)
        {
            var newCenter = matrix.TransformPoint(Center);

            // Извлечение масштаба (для не-uniform масштаба преобразуем в эллипс)
            double scaleX = Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M21 * matrix.M21);
            double scaleY = Math.Sqrt(matrix.M12 * matrix.M12 + matrix.M22 * matrix.M22);

            if (Math.Abs(scaleX - scaleY) < 1e-10)
            {
                // Uniform scale — остаётся окружностью
                return new CircleShape(newCenter, Radius * scaleX)
                {
                    Id = Id
                };
            }

            // Non-uniform scale — становится эллипсом
            double rotation = Math.Atan2(matrix.M21, matrix.M11) * 180 / Math.PI;
            return new EllipseShape(newCenter, Radius * scaleX, Radius * scaleY, rotation)
            {
                Id = Id
            };
        }

        /// <summary>
        /// Проверяет, находится ли точка внутри окружности.
        /// </summary>
        public bool Contains(Point3D point)
        {
            return Center.DistanceTo2D(point) <= Radius;
        }

        /// <summary>
        /// Возвращает точку на окружности по углу (в радианах).
        /// </summary>
        public Point3D GetPointAtAngle(double angleRadians)
        {
            return new Point3D(
                Center.X + Radius * Math.Cos(angleRadians),
                Center.Y + Radius * Math.Sin(angleRadians),
                Center.Z);
        }

        public override string ToString() => $"Circle(center={Center}, r={Radius})";
    }
}

