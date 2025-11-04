using CadProjector.Core.Objects;
using CadProjector.Rendering.Transforms;
using System.Collections.Concurrent;

namespace CadProjector.Rendering.Threading
{
    internal class GeometryProcessingTask : RenderTask
    {
        private readonly IUidObject obj;
        private readonly ConcurrentDictionary<Guid, LinesCollection> resultCache;

        public GeometryProcessingTask(IUidObject obj, ConcurrentDictionary<Guid, LinesCollection> resultCache)
        {
            this.obj = obj;
            this.resultCache = resultCache;
        }

        public override void Execute()
        {
            if (obj is IRenderObject renderObj)
            {
                var lines = renderObj.GetRenderLines();
                if (lines != null)
                {
                    resultCache.TryAdd(obj.Uid, lines);
                }
            }
        }
    }

    internal class TransformProcessingTask : RenderTask
    {
        private readonly LinesCollection lines;
        private readonly IEnumerable<ITransform> transforms;
        private readonly ConcurrentDictionary<Guid, LinesCollection> resultCache;
        private readonly Guid resultId;

        public TransformProcessingTask(
 LinesCollection lines,
            IEnumerable<ITransform> transforms,
       ConcurrentDictionary<Guid, LinesCollection> resultCache,
         Guid resultId)
        {
            this.lines = lines;
            this.transforms = transforms;
            this.resultCache = resultCache;
            this.resultId = resultId;
        }

        public override void Execute()
        {
            if (lines == null || !lines.Any()) return;

            var result = new LinesCollection();
            var processedLines = new ConcurrentBag<VectorLine>();

            Parallel.ForEach(lines, line =>
              {
                  var transformedLine = line;
                  foreach (var transform in transforms)
                  {
                      var p1 = transform.Transform(transformedLine.P1);
                      transformedLine = new VectorLine(
                         p1,
                         transform.Transform(transformedLine.Vector),
                    transformedLine.Length,
                      transformedLine.IsBlank)
                      {
                          T1 = transformedLine.T1,
                          T2 = transformedLine.T2
                      };
                  }
                  processedLines.Add(transformedLine);
              });

            result.AddRange(processedLines.OrderBy(l => l.P1.X));
            resultCache.TryAdd(resultId, result);
        }
    }
}