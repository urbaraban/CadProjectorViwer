using CadProjector.FileFormats.Geometry.Shapes;

namespace CadProjector.FileFormats.Geometry.Segments
{
    /// <summary>
    /// Направление обхода дуги.
    /// </summary>
    public enum SweepDirection
    {
        /// <summary>Против часовой стрелки (positive-angle).</summary>
        Counterclockwise = 0,

        /// <summary>По часовой стрелке (negative-angle).</summary>
        Clockwise = 1
    }

    /// <summary>
    /// Сегмент дуги эллипса.
    /// SVG: A (elliptical arc).
    /// </summary>
    public sealed class ArcSegment : IPathSegment
    {
        public PathSegmentType Type => PathSegmentType.Arc;

        public Point3D EndPoint { get; }

        /// <summary>
        /// Радиус по оси X.
        /// </summary>
        public double RadiusX { get; }

        /// <summary>
        /// Радиус по оси Y.
        /// </summary>
        public double RadiusY { get; }

        /// <summary>
        /// Угол поворота эллипса относительно оси X (в градусах).
        /// </summary>
        public double RotationAngle { get; }

        /// <summary>
        /// Использовать большую дугу (large-arc-flag в SVG).
        /// </summary>
        public bool IsLargeArc { get; }

        /// <summary>
        /// Направление обхода (sweep-flag в SVG).
        /// </summary>
        public SweepDirection SweepDirection { get; }

        public ArcSegment(
            Point3D endPoint,
            double radiusX,
            double radiusY,
            double rotationAngle = 0,
            bool isLargeArc = false,
            SweepDirection sweepDirection = SweepDirection.Counterclockwise)
        {
            EndPoint = endPoint;
            RadiusX = radiusX;
            RadiusY = radiusY;
            RotationAngle = rotationAngle;
            IsLargeArc = isLargeArc;
            SweepDirection = sweepDirection;
        }

        public IReadOnlyList<Point3D> Tessellate(Point3D startPoint, double tolerance)
        {
            var result = new List<Point3D>();

            // Конвертация дуги в параметрическую форму
            if (!TryGetArcParameters(startPoint, out var center, out double startAngle, out double deltaAngle))
            {
                // Если дуга вырождается в линию
                result.Add(EndPoint);
                return result;
            }

            // Вычисление количества сегментов на основе радиуса и длины дуги
            double radius = Math.Max(RadiusX, RadiusY);
            double arcLength = Math.Abs(deltaAngle) * radius;
            int segments = Math.Max(2, (int)Math.Ceiling(arcLength / tolerance));

            double rotationRad = RotationAngle * Math.PI / 180.0;
            double cosRotation = Math.Cos(rotationRad);
            double sinRotation = Math.Sin(rotationRad);

            for (int i = 1; i <= segments; i++)
            {
                double t = (double)i / segments;
                double angle = startAngle + deltaAngle * t;

                // Точка на эллипсе
                double x = RadiusX * Math.Cos(angle);
                double y = RadiusY * Math.Sin(angle);

                // Применяем поворот и смещение к центру
                double px = center.X + x * cosRotation - y * sinRotation;
                double py = center.Y + x * sinRotation + y * cosRotation;

                result.Add(new Point3D(px, py, startPoint.Z));
            }

            return result;
        }

        /// <summary>
        /// Вычисляет параметры дуги (центр, начальный угол, дельта угла).
        /// Реализация согласно спецификации SVG.
        /// </summary>
        private bool TryGetArcParameters(Point3D startPoint, out Point3D center, out double startAngle, out double deltaAngle)
        {
            center = Point3D.Zero;
            startAngle = 0;
            deltaAngle = 0;

            double rx = Math.Abs(RadiusX);
            double ry = Math.Abs(RadiusY);

            if (rx < 1e-10 || ry < 1e-10)
                return false;

            double x1 = startPoint.X;
            double y1 = startPoint.Y;
            double x2 = EndPoint.X;
            double y2 = EndPoint.Y;

            // Если точки совпадают
            if (Math.Abs(x1 - x2) < 1e-10 && Math.Abs(y1 - y2) < 1e-10)
                return false;

            double phi = RotationAngle * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);

            // Шаг 1: вычисляем (x1', y1')
            double dx = (x1 - x2) / 2;
            double dy = (y1 - y2) / 2;
            double x1p = cosPhi * dx + sinPhi * dy;
            double y1p = -sinPhi * dx + cosPhi * dy;

            // Корректировка радиусов если дуга невозможна
            double lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry);
            if (lambda > 1)
            {
                double lambdaSqrt = Math.Sqrt(lambda);
                rx *= lambdaSqrt;
                ry *= lambdaSqrt;
            }

            // Шаг 2: вычисляем (cx', cy')
            double sq = Math.Max(0,
                (rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p) /
                (rx * rx * y1p * y1p + ry * ry * x1p * x1p));
            double coef = Math.Sqrt(sq) * (IsLargeArc == (SweepDirection == SweepDirection.Clockwise) ? -1 : 1);

            double cxp = coef * rx * y1p / ry;
            double cyp = -coef * ry * x1p / rx;

            // Шаг 3: вычисляем (cx, cy) из (cx', cy')
            double cx = cosPhi * cxp - sinPhi * cyp + (x1 + x2) / 2;
            double cy = sinPhi * cxp + cosPhi * cyp + (y1 + y2) / 2;

            center = new Point3D(cx, cy, startPoint.Z);

            // Шаг 4: вычисляем углы
            double ux = (x1p - cxp) / rx;
            double uy = (y1p - cyp) / ry;
            double vx = (-x1p - cxp) / rx;
            double vy = (-y1p - cyp) / ry;

            // Начальный угол
            double n = Math.Sqrt(ux * ux + uy * uy);
            startAngle = (uy >= 0 ? 1 : -1) * Math.Acos(Math.Clamp(ux / n, -1, 1));

            // Дельта угла
            n = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            double p = ux * vx + uy * vy;
            double sign = (ux * vy - uy * vx) >= 0 ? 1 : -1;
            deltaAngle = sign * Math.Acos(Math.Clamp(p / n, -1, 1));

            // Корректировка для sweep direction
            if (SweepDirection == SweepDirection.Counterclockwise && deltaAngle < 0)
                deltaAngle += 2 * Math.PI;
            else if (SweepDirection == SweepDirection.Clockwise && deltaAngle > 0)
                deltaAngle -= 2 * Math.PI;

            return true;
        }

        public IPathSegment Transform(Matrix3x3 matrix)
        {
            // Трансформация дуги — сложная операция, упрощённая версия
            // В идеале нужно пересчитать радиусы с учётом масштаба/скоса
            var newEndPoint = matrix.TransformPoint(EndPoint);

            // Извлечение масштаба из матрицы
            double scaleX = Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M21 * matrix.M21);
            double scaleY = Math.Sqrt(matrix.M12 * matrix.M12 + matrix.M22 * matrix.M22);

            return new ArcSegment(
                newEndPoint,
                RadiusX * scaleX,
                RadiusY * scaleY,
                RotationAngle,
                IsLargeArc,
                SweepDirection);
        }

        public override string ToString() =>
            $"A {RadiusX},{RadiusY} {RotationAngle} {(IsLargeArc ? 1 : 0)},{(int)SweepDirection} {EndPoint}";
    }
}

