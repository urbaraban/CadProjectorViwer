using System.Collections;

namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Коллекция фигур — результат парсинга файла.
    /// </summary>
    public sealed class ShapeCollection : IList<IShape>
    {
        private readonly List<IShape> _shapes;

        /// <summary>
        /// Имя файла или документа.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Единицы измерения документа.
        /// </summary>
        public string? Units { get; set; }

        /// <summary>
        /// Метаданные документа.
        /// </summary>
        public Dictionary<string, string> Metadata { get; } = new();

        public BoundingBox Bounds
        {
            get
            {
                if (_shapes.Count == 0)
                    return BoundingBox.Empty;

                var result = _shapes[0].Bounds;
                for (int i = 1; i < _shapes.Count; i++)
                {
                    result = result.Union(_shapes[i].Bounds);
                }
                return result;
            }
        }

        public ShapeCollection()
        {
            _shapes = new List<IShape>();
        }

        public ShapeCollection(string fileName)
        {
            _shapes = new List<IShape>();
            FileName = fileName;
        }

        public ShapeCollection(IEnumerable<IShape> shapes)
        {
            _shapes = shapes.ToList();
        }

        /// <summary>
        /// Тесселирует все фигуры и возвращает списки точек.
        /// </summary>
        public List<IReadOnlyList<Point3D>> TessellateAll(double tolerance = 1.0)
        {
            var result = new List<IReadOnlyList<Point3D>>();
            foreach (var shape in _shapes)
            {
                var points = shape.Tessellate(tolerance);
                if (points.Count > 0)
                {
                    result.Add(points);
                }
            }
            return result;
        }

        /// <summary>
        /// Возвращает все фигуры (включая вложенные группы) плоским списком.
        /// </summary>
        public IEnumerable<IShape> Flatten()
        {
            foreach (var shape in _shapes)
            {
                if (shape is ShapeGroup group)
                {
                    foreach (var nested in group.Flatten())
                    {
                        yield return nested;
                    }
                }
                else
                {
                    yield return shape;
                }
            }
        }

        /// <summary>
        /// Возвращает все фигуры определённого типа.
        /// </summary>
        public IEnumerable<T> GetShapesOfType<T>() where T : IShape
        {
            return Flatten().OfType<T>();
        }

        /// <summary>
        /// Применяет трансформацию ко всем фигурам.
        /// </summary>
        public ShapeCollection Transform(Matrix3x3 matrix)
        {
            var result = new ShapeCollection(FileName ?? string.Empty)
            {
                Units = Units
            };

            foreach (var kvp in Metadata)
            {
                result.Metadata[kvp.Key] = kvp.Value;
            }

            foreach (var shape in _shapes)
            {
                result.Add(shape.Transform(matrix));
            }

            return result;
        }

        public override string ToString() => 
            $"ShapeCollection \"{FileName ?? "unnamed"}\" ({_shapes.Count} shapes)";

        #region IList<IShape> Implementation

        public IShape this[int index]
        {
            get => _shapes[index];
            set => _shapes[index] = value;
        }

        public int Count => _shapes.Count;

        public bool IsReadOnly => false;

        public void Add(IShape item) => _shapes.Add(item);

        public void AddRange(IEnumerable<IShape> items) => _shapes.AddRange(items);

        public void Clear() => _shapes.Clear();

        public bool Contains(IShape item) => _shapes.Contains(item);

        public void CopyTo(IShape[] array, int arrayIndex) => _shapes.CopyTo(array, arrayIndex);

        public IEnumerator<IShape> GetEnumerator() => _shapes.GetEnumerator();

        public int IndexOf(IShape item) => _shapes.IndexOf(item);

        public void Insert(int index, IShape item) => _shapes.Insert(index, item);

        public bool Remove(IShape item) => _shapes.Remove(item);

        public void RemoveAt(int index) => _shapes.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}

