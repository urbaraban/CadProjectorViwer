namespace CadProjector.FileFormats.Geometry
{
    /// <summary>
    /// Представляет вектор в 3D пространстве.
    /// </summary>
    public readonly struct Vector3D : IEquatable<Vector3D>
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public static Vector3D Zero => new(0, 0, 0);
        public static Vector3D UnitX => new(1, 0, 0);
        public static Vector3D UnitY => new(0, 1, 0);
        public static Vector3D UnitZ => new(0, 0, 1);

        public Vector3D(double x, double y, double z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public double LengthSquared => X * X + Y * Y + Z * Z;

        public Vector3D Normalize()
        {
            double len = Length;
            if (len < 1e-10)
                return Zero;
            return new Vector3D(X / len, Y / len, Z / len);
        }

        public static double Dot(Vector3D a, Vector3D b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vector3D Cross(Vector3D a, Vector3D b)
        {
            return new Vector3D(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        /// <summary>
        /// Угол между двумя векторами в радианах.
        /// </summary>
        public static double AngleBetween(Vector3D a, Vector3D b)
        {
            double dot = Dot(a.Normalize(), b.Normalize());
            dot = Math.Clamp(dot, -1.0, 1.0);
            return Math.Acos(dot);
        }

        /// <summary>
        /// Угол между двумя 2D векторами в градусах (с учётом знака).
        /// </summary>
        public static double AngleBetween2D(Vector3D a, Vector3D b)
        {
            double angle = Math.Atan2(b.Y, b.X) - Math.Atan2(a.Y, a.X);
            return angle * 180.0 / Math.PI;
        }

        public bool Equals(Vector3D other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object? obj) => obj is Vector3D other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public static bool operator ==(Vector3D left, Vector3D right) => left.Equals(right);

        public static bool operator !=(Vector3D left, Vector3D right) => !left.Equals(right);

        public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3D operator *(Vector3D v, double scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

        public static Vector3D operator *(double scalar, Vector3D v) => v * scalar;

        public static Vector3D operator /(Vector3D v, double scalar) => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

        public static Vector3D operator -(Vector3D v) => new(-v.X, -v.Y, -v.Z);

        public override string ToString() => $"<{X:F3}, {Y:F3}, {Z:F3}>";
    }
}

