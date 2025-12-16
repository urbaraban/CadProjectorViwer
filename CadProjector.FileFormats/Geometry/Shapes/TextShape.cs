namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Текстовый элемент.
    /// Соответствует SVG &lt;text&gt; элементу.
    /// </summary>
    public sealed class TextShape : IShape
    {
        public string? Id { get; set; }

        /// <summary>
        /// Текстовое содержимое.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Позиция текста (базовая линия).
        /// </summary>
        public Point3D Position { get; }

        /// <summary>
        /// Размер шрифта.
        /// </summary>
        public double FontSize { get; }

        /// <summary>
        /// Имя шрифта.
        /// </summary>
        public string FontFamily { get; }

        /// <summary>
        /// Угол поворота текста в градусах.
        /// </summary>
        public double Rotation { get; }

        public bool IsClosed => true;

        public BoundingBox Bounds
        {
            get
            {
                // Приблизительный bounding box на основе размера шрифта
                double width = Text.Length * FontSize * 0.6; // Приблизительная ширина
                double height = FontSize;
                return new BoundingBox(
                    Position,
                    new Point3D(Position.X + width, Position.Y + height, Position.Z));
            }
        }

        public TextShape(string text, Point3D position, double fontSize = 12, string fontFamily = "Arial")
        {
            Text = text ?? string.Empty;
            Position = position;
            FontSize = fontSize;
            FontFamily = fontFamily;
            Rotation = 0;
        }

        public TextShape(string text, double x, double y, double fontSize = 12, string fontFamily = "Arial", double z = 0)
            : this(text, new Point3D(x, y, z), fontSize, fontFamily)
        {
        }

        private TextShape(string text, Point3D position, double fontSize, string fontFamily, double rotation, string? id)
        {
            Text = text;
            Position = position;
            FontSize = fontSize;
            FontFamily = fontFamily;
            Rotation = rotation;
            Id = id;
        }

        public IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0)
        {
            // Текст не тесселируется в точки — это символический элемент
            // Возвращаем позицию как единственную точку
            return new[] { Position };
        }

        public IShape Transform(Matrix3x3 matrix)
        {
            var newPosition = matrix.TransformPoint(Position);

            // Извлечение масштаба и угла поворота из матрицы
            double scaleX = Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M21 * matrix.M21);
            double newRotation = Rotation + Math.Atan2(matrix.M21, matrix.M11) * 180 / Math.PI;

            return new TextShape(
                Text,
                newPosition,
                FontSize * scaleX,
                FontFamily,
                newRotation,
                Id);
        }

        public override string ToString() => $"Text(\"{Text}\" at {Position})";
    }
}

