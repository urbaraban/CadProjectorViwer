using System.Collections;

namespace CadProjector.FileFormats.Geometry.Shapes
{
    /// <summary>
    /// Группа фигур.
    /// Соответствует SVG &lt;g&gt; элементу.
    /// </summary>
    public sealed class ShapeGroup : IShape, IList<IShape>
    {
        private readonly List<IShape> _shapes;

        public string? Id { get; set; }

        /// <summary>
        /// Имя группы (например, имя слоя).
        /// </summary>
        public string? Name { get; set; }

        public bool IsClosed => false;

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

        public ShapeGroup()
        {
            _shapes = new List<IShape>();
        }

        public ShapeGroup(string name)
        {
            _shapes = new List<IShape>();
            Name = name;
        }

        public ShapeGroup(IEnumerable<IShape> shapes)
        {
            _shapes = shapes.ToList();
        }

        public ShapeGroup(string name, IEnumerable<IShape> shapes)
        {
            _shapes = shapes.ToList();
            Name = name;
        }

        public IReadOnlyList<Point3D> Tessellate(double tolerance = 1.0)
        {
            var result = new List<Point3D>();
            foreach (var shape in _shapes)
            {
                result.AddRange(shape.Tessellate(tolerance));
            }
            return result;
        }

        public IShape Transform(Matrix3x3 matrix)
        {
            var newShapes = _shapes.Select(s => s.Transform(matrix));
            return new ShapeGroup(Name ?? string.Empty, newShapes)
            {
                Id = Id
            };
        }

        /// <summary>
        /// Возвращает все фигуры в группе (включая вложенные группы) плоским списком.
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

        public override string ToString() => $"Group \"{Name ?? Id ?? "unnamed"}\" ({_shapes.Count} shapes)";

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

