namespace CadProjector.FileFormats.Geometry
{
    /// <summary>
    /// Матрица трансформации 3x3 для 2D преобразований с однородными координатами.
    /// | M11 M12 M13 |
    /// | M21 M22 M23 |
    /// | M31 M32 M33 |
    /// </summary>
    public readonly struct Matrix3x3 : IEquatable<Matrix3x3>
    {
        public double M11 { get; }
        public double M12 { get; }
        public double M13 { get; }
        public double M21 { get; }
        public double M22 { get; }
        public double M23 { get; }
        public double M31 { get; }
        public double M32 { get; }
        public double M33 { get; }

        public static Matrix3x3 Identity => new(1, 0, 0, 0, 1, 0, 0, 0, 1);

        public Matrix3x3(
            double m11, double m12, double m13,
            double m21, double m22, double m23,
            double m31, double m32, double m33)
        {
            M11 = m11; M12 = m12; M13 = m13;
            M21 = m21; M22 = m22; M23 = m23;
            M31 = m31; M32 = m32; M33 = m33;
        }

        /// <summary>
        /// Создаёт матрицу переноса.
        /// </summary>
        public static Matrix3x3 CreateTranslation(double dx, double dy)
        {
            return new Matrix3x3(
                1, 0, dx,
                0, 1, dy,
                0, 0, 1);
        }

        /// <summary>
        /// Создаёт матрицу масштабирования.
        /// </summary>
        public static Matrix3x3 CreateScale(double sx, double sy)
        {
            return new Matrix3x3(
                sx, 0, 0,
                0, sy, 0,
                0, 0, 1);
        }

        /// <summary>
        /// Создаёт матрицу масштабирования относительно точки.
        /// </summary>
        public static Matrix3x3 CreateScale(double sx, double sy, Point3D center)
        {
            return CreateTranslation(-center.X, -center.Y)
                .Multiply(CreateScale(sx, sy))
                .Multiply(CreateTranslation(center.X, center.Y));
        }

        /// <summary>
        /// Создаёт матрицу поворота (угол в радианах).
        /// </summary>
        public static Matrix3x3 CreateRotation(double angleRadians)
        {
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);
            return new Matrix3x3(
                cos, -sin, 0,
                sin, cos, 0,
                0, 0, 1);
        }

        /// <summary>
        /// Создаёт матрицу поворота относительно точки.
        /// </summary>
        public static Matrix3x3 CreateRotation(double angleRadians, Point3D center)
        {
            return CreateTranslation(-center.X, -center.Y)
                .Multiply(CreateRotation(angleRadians))
                .Multiply(CreateTranslation(center.X, center.Y));
        }

        /// <summary>
        /// Создаёт матрицу скоса (shear/skew).
        /// </summary>
        public static Matrix3x3 CreateSkew(double angleXRadians, double angleYRadians)
        {
            return new Matrix3x3(
                1, Math.Tan(angleXRadians), 0,
                Math.Tan(angleYRadians), 1, 0,
                0, 0, 1);
        }

        /// <summary>
        /// Умножает матрицу на другую матрицу.
        /// </summary>
        public Matrix3x3 Multiply(Matrix3x3 other)
        {
            return new Matrix3x3(
                M11 * other.M11 + M12 * other.M21 + M13 * other.M31,
                M11 * other.M12 + M12 * other.M22 + M13 * other.M32,
                M11 * other.M13 + M12 * other.M23 + M13 * other.M33,

                M21 * other.M11 + M22 * other.M21 + M23 * other.M31,
                M21 * other.M12 + M22 * other.M22 + M23 * other.M32,
                M21 * other.M13 + M22 * other.M23 + M23 * other.M33,

                M31 * other.M11 + M32 * other.M21 + M33 * other.M31,
                M31 * other.M12 + M32 * other.M22 + M33 * other.M32,
                M31 * other.M13 + M32 * other.M23 + M33 * other.M33);
        }

        /// <summary>
        /// Применяет матрицу к точке.
        /// </summary>
        public Point3D TransformPoint(Point3D point)
        {
            double x = M11 * point.X + M12 * point.Y + M13;
            double y = M21 * point.X + M22 * point.Y + M23;
            // Z сохраняется без изменений (2D трансформация)
            return new Point3D(x, y, point.Z);
        }

        /// <summary>
        /// Применяет матрицу к вектору (без учёта переноса).
        /// </summary>
        public Vector3D TransformVector(Vector3D vector)
        {
            double x = M11 * vector.X + M12 * vector.Y;
            double y = M21 * vector.X + M22 * vector.Y;
            return new Vector3D(x, y, vector.Z);
        }

        public double Determinant =>
            M11 * (M22 * M33 - M23 * M32) -
            M12 * (M21 * M33 - M23 * M31) +
            M13 * (M21 * M32 - M22 * M31);

        /// <summary>
        /// Возвращает обратную матрицу.
        /// </summary>
        public Matrix3x3 Invert()
        {
            double det = Determinant;
            if (Math.Abs(det) < 1e-10)
                throw new InvalidOperationException("Matrix is singular and cannot be inverted.");

            double invDet = 1.0 / det;

            return new Matrix3x3(
                (M22 * M33 - M23 * M32) * invDet,
                (M13 * M32 - M12 * M33) * invDet,
                (M12 * M23 - M13 * M22) * invDet,

                (M23 * M31 - M21 * M33) * invDet,
                (M11 * M33 - M13 * M31) * invDet,
                (M13 * M21 - M11 * M23) * invDet,

                (M21 * M32 - M22 * M31) * invDet,
                (M12 * M31 - M11 * M32) * invDet,
                (M11 * M22 - M12 * M21) * invDet);
        }

        public bool Equals(Matrix3x3 other)
        {
            return M11.Equals(other.M11) && M12.Equals(other.M12) && M13.Equals(other.M13) &&
                   M21.Equals(other.M21) && M22.Equals(other.M22) && M23.Equals(other.M23) &&
                   M31.Equals(other.M31) && M32.Equals(other.M32) && M33.Equals(other.M33);
        }

        public override bool Equals(object? obj) => obj is Matrix3x3 other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(M11); hash.Add(M12); hash.Add(M13);
            hash.Add(M21); hash.Add(M22); hash.Add(M23);
            hash.Add(M31); hash.Add(M32); hash.Add(M33);
            return hash.ToHashCode();
        }

        public static bool operator ==(Matrix3x3 left, Matrix3x3 right) => left.Equals(right);

        public static bool operator !=(Matrix3x3 left, Matrix3x3 right) => !left.Equals(right);

        public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b) => a.Multiply(b);

        public override string ToString() =>
            $"[{M11:F3}, {M12:F3}, {M13:F3}]\n" +
            $"[{M21:F3}, {M22:F3}, {M23:F3}]\n" +
            $"[{M31:F3}, {M32:F3}, {M33:F3}]";
    }
}

