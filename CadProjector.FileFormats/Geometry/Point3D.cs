namespace CadProjector.FileFormats.Geometry
{
    /// <summary>
    /// Представляет точку в 3D пространстве.
    /// Платформо-независимая реализация без привязки к WPF.
    /// </summary>
    public readonly struct Point3D : IEquatable<Point3D>
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public static Point3D Zero => new(0, 0, 0);

        public Point3D(double x, double y, double z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Расстояние до другой точки в 3D пространстве.
        /// </summary>
        public double DistanceTo(Point3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Расстояние до другой точки в 2D пространстве (игнорируя Z).
        /// </summary>
        public double DistanceTo2D(Point3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public Point3D Offset(double dx, double dy, double dz = 0)
        {
            return new Point3D(X + dx, Y + dy, Z + dz);
        }

        public Point3D Scale(double factor)
        {
            return new Point3D(X * factor, Y * factor, Z * factor);
        }

        /// <summary>
        /// Линейная интерполяция между двумя точками.
        /// </summary>
        public static Point3D Lerp(Point3D a, Point3D b, double t)
        {
            return new Point3D(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t);
        }

        public bool Equals(Point3D other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object? obj) => obj is Point3D other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public static bool operator ==(Point3D left, Point3D right) => left.Equals(right);

        public static bool operator !=(Point3D left, Point3D right) => !left.Equals(right);

        public static Point3D operator +(Point3D a, Vector3D v) => new(a.X + v.X, a.Y + v.Y, a.Z + v.Z);

        public static Point3D operator -(Point3D a, Vector3D v) => new(a.X - v.X, a.Y - v.Y, a.Z - v.Z);

        public static Vector3D operator -(Point3D a, Point3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";
    }
}

