using System;
using System.Collections.Generic;
using CadProjector.Core.Primitives;
using CadProjector.Rendering;

namespace CadProjector.Devices.Modules.Optimization
{
    /// <summary>
    /// Жадный алгоритм кратчайшего пути (Nearest Neighbor TSP).
    /// На каждом шаге берётся отрезок, ближайший конец которого находится
    /// ближе всего к текущей позиции луча. При необходимости отрезок разворачивается.
    /// </summary>
    public class FindShortestPath : DeviceModule
    {
        public override string Name => "ShortestPath";
        public override string Description => "Path optimization to reduce rendering time";

        public LinesCollection Optimize(IEnumerable<VectorLine> lines)
        {
            if (!IsEnabled || lines == null)
                return new LinesCollection(lines ?? Array.Empty<VectorLine>());

            // Рабочий пул: храним индексы, удаляем через swap-with-last ? O(1)
            var pool = new List<VectorLine>(lines);
            int n = pool.Count;

            if (n <= 1)
                return new LinesCollection(pool);

            var result = new List<VectorLine>(n);

            // Стартуем с первого отрезка как есть
            result.Add(pool[0]);
            SwapRemove(pool, 0);

            while (pool.Count > 0)
            {
                var currentEnd = result[^1].P2;

                int bestIdx = 0;
                bool bestReverse = false;
                double bestDistSq = double.MaxValue;

                for (int i = 0; i < pool.Count; i++)
                {
                    double d0 = DistSq(currentEnd, pool[i].P1);
                    double d1 = DistSq(currentEnd, pool[i].P2);

                    if (d0 < bestDistSq) { bestDistSq = d0; bestIdx = i; bestReverse = false; }
                    if (d1 < bestDistSq) { bestDistSq = d1; bestIdx = i; bestReverse = true; }
                }

                var next = bestReverse ? pool[bestIdx].Reverse() : pool[bestIdx];
                result.Add(next);
                SwapRemove(pool, bestIdx);
            }

            return new LinesCollection(result);
        }

        private static void SwapRemove(List<VectorLine> list, int idx)
        {
            list[idx] = list[^1];
            list.RemoveAt(list.Count - 1);
        }

        private static double DistSq(RenderPoint a, RenderPoint b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy;
        }
    }
}
