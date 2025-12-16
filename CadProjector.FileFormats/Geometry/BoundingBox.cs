namespace CadProjector.FileFormats.Geometry
{
    /// <summary>
    /// Ограничивающий прямоугольник в 3D пространстве.
    /// </summary>
    public readonly struct BoundingBox : IEquatable<BoundingBox>
    {
        public Point3D Min { get; }
        public Point3D Max { get; }

        public double Width => Max.X - Min.X;
        public double Height => Max.Y - Min.Y;
        public double Depth => Max.Z - Min.Z;

        public Point3D Center => new(
            (Min.X + Max.X) / 2,
            (Min.Y + Max.Y) / 2,
            (Min.Z + Max.Z) / 2);

        public bool IsEmpty => Width <= 0 || Height <= 0;

        public static BoundingBox Empty => new(Point3D.Zero, Point3D.Zero);

        public BoundingBox(Point3D min, Point3D max)
        {
            Min = min;
            Max = max;
        }

        public BoundingBox(double x, double y, double width, double height)
        {
            Min = new Point3D(x, y, 0);
            Max = new Point3D(x + width, y + height, 0);
        }

        /// <summary>
        /// Создаёт BoundingBox из коллекции точек.
        /// </summary>
        public static BoundingBox FromPoints(IEnumerable<Point3D> points)
        {
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

            bool hasPoints = false;
            foreach (var p in points)
            {
                hasPoints = true;
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Z < minZ) minZ = p.Z;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
                if (p.Z > maxZ) maxZ = p.Z;
            }

            if (!hasPoints)
                return Empty;

            return new BoundingBox(new Point3D(minX, minY, minZ), new Point3D(maxX, maxY, maxZ));
        }

        /// <summary>
        /// Объединяет два BoundingBox.
        /// </summary>
        public BoundingBox Union(BoundingBox other)
        {
            if (IsEmpty) return other;
            if (other.IsEmpty) return this;

            return new BoundingBox(
                new Point3D(
                    Math.Min(Min.X, other.Min.X),
                    Math.Min(Min.Y, other.Min.Y),
                    Math.Min(Min.Z, other.Min.Z)),
                new Point3D(
                    Math.Max(Max.X, other.Max.X),
                    Math.Max(Max.Y, other.Max.Y),
                    Math.Max(Max.Z, other.Max.Z)));
        }

        /// <summary>
        /// Проверяет, содержится ли точка внутри BoundingBox.
        /// </summary>
        public bool Contains(Point3D point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        /// <summary>
        /// Расширяет BoundingBox на заданную величину.
        /// </summary>
        public BoundingBox Inflate(double amount)
        {
            return new BoundingBox(
                new Point3D(Min.X - amount, Min.Y - amount, Min.Z - amount),
                new Point3D(Max.X + amount, Max.Y + amount, Max.Z + amount));
        }

        public bool Equals(BoundingBox other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        public override bool Equals(object? obj) => obj is BoundingBox other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Min, Max);

        public static bool operator ==(BoundingBox left, BoundingBox right) => left.Equals(right);

        public static bool operator !=(BoundingBox left, BoundingBox right) => !left.Equals(right);

        public override string ToString() => $"[{Min} - {Max}]";
    }
}

