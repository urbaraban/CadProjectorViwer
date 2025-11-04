using System;
using System.Collections.Generic;
using CadProjector.Core.Primitives;
using System.Linq;

namespace CadProjector.Rendering
{
    public class LinesCollection : List<VectorLine>, IRenderedObject
    {
        public bool IsClosed { get; set; }

        public LinesCollection() { }

        public LinesCollection(IEnumerable<VectorLine> lines, bool isClosed = false)
      {
            this.AddRange(lines);
            this.IsClosed = isClosed;
     }

        public static LinesCollection Convert(RenderPoint[] points, bool isBlank = false)
 {
       if (points.Length < 2)
                return new LinesCollection();

  var lines = new List<VectorLine>();
   for (int i = 0; i < points.Length - 1; i++)
            {
 lines.Add(new VectorLine(points[i], points[i + 1], isBlank));
            }

        return new LinesCollection(lines);
      }

        public LinesCollection Clone()
        {
            return new LinesCollection(
      this.Select(x => new VectorLine(x.P1, x.P2, x.IsBlank)),
     this.IsClosed);
     }

        public void Reverse()
 {
            this.Reverse();
            for (int i = 0; i < this.Count; i++)
        {
     this[i] = this[i].Reverse();
          }
     }
    }
}