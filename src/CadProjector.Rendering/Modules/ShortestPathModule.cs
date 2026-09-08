namespace CadProjector.Rendering.Modules;

/// <summary>Chain + NN + 2-opt path order (legacy FindShortestPath). Skips blanks.</summary>
public sealed class ShortestPathModule : IFrameModule
{
    public string Name => "Shortest Path";
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "Connection tolerance", Min = 0, Increment = 1e-4, Format = "0.#######")]
    public double ConnectionTolerance { get; set; } = 1e-3;

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || input.Points.Count < 2)
            return input;

        var pool = StrokeSegmentOps.ToSegments(input).Where(s => !s.IsBlank).ToList();
        if (pool.Count <= 1)
            return StrokeSegmentOps.FromSegments(pool);

        var chains = BuildChains(pool);
        List<StrokeSegment> result;
        if (chains.Count == 1)
            result = chains[0];
        else
        {
            var ordered = OrderChains(chains);
            result = new List<StrokeSegment>(pool.Count);
            foreach (var c in ordered)
                result.AddRange(c);
        }

        return StrokeSegmentOps.FromSegments(result);
    }

    private List<List<StrokeSegment>> BuildChains(List<StrokeSegment> pool)
    {
        var tolSq = ConnectionTolerance * ConnectionTolerance;
        var used = new bool[pool.Count];
        var chains = new List<List<StrokeSegment>>();

        for (var seed = 0; seed < pool.Count; seed++)
        {
            if (used[seed]) continue;
            var chain = new LinkedList<StrokeSegment>();
            chain.AddLast(pool[seed]);
            used[seed] = true;

            for (;;)
            {
                var best = FindNearest(pool, used, chain.Last!.Value.P2, tolSq, out var rev);
                if (best < 0) break;
                chain.AddLast(rev ? pool[best].Reverse() : pool[best]);
                used[best] = true;
            }

            for (;;)
            {
                var best = FindNearest(pool, used, chain.First!.Value.P1, tolSq, out var rev);
                if (best < 0) break;
                chain.AddFirst(rev ? pool[best] : pool[best].Reverse());
                used[best] = true;
            }

            chains.Add(new List<StrokeSegment>(chain));
        }

        return chains;
    }

    private static int FindNearest(List<StrokeSegment> pool, bool[] used, RenderPoint tip, double maxDistSq, out bool rev)
    {
        var bestIdx = -1;
        rev = false;
        var best = maxDistSq;
        for (var i = 0; i < pool.Count; i++)
        {
            if (used[i]) continue;
            var d0 = StrokeSegmentOps.DistSq(tip, pool[i].P1);
            var d1 = StrokeSegmentOps.DistSq(tip, pool[i].P2);
            if (d0 < best) { best = d0; bestIdx = i; rev = false; }
            if (d1 < best) { best = d1; bestIdx = i; rev = true; }
        }
        return bestIdx;
    }

    private static List<List<StrokeSegment>> OrderChains(List<List<StrokeSegment>> chains)
    {
        var n = chains.Count;
        var used = new bool[n];
        var ordered = new List<List<StrokeSegment>>(n) { chains[0] };
        used[0] = true;

        for (var step = 1; step < n; step++)
        {
            var tip = ordered[^1][^1].P2;
            var bestIdx = -1;
            var bestRev = false;
            var bestDist = double.MaxValue;
            for (var i = 0; i < n; i++)
            {
                if (used[i]) continue;
                var d0 = StrokeSegmentOps.DistSq(tip, chains[i][0].P1);
                var d1 = StrokeSegmentOps.DistSq(tip, chains[i][^1].P2);
                if (d0 < bestDist) { bestDist = d0; bestIdx = i; bestRev = false; }
                if (d1 < bestDist) { bestDist = d1; bestIdx = i; bestRev = true; }
            }
            if (bestIdx < 0) break;
            ordered.Add(bestRev ? ReverseChain(chains[bestIdx]) : chains[bestIdx]);
            used[bestIdx] = true;
        }

        TwoOptChains(ordered);
        return ordered;
    }

    private static void TwoOptChains(List<List<StrokeSegment>> chains)
    {
        var n = chains.Count;
        var improved = true;
        while (improved)
        {
            improved = false;
            for (var i = 0; i < n - 2; i++)
            for (var j = i + 2; j < n; j++)
            {
                var dBefore = StrokeSegmentOps.DistSq(chains[i][^1].P2, chains[i + 1][0].P1);
                var dAfter = StrokeSegmentOps.DistSq(chains[i][^1].P2, chains[j][^1].P2);
                if (j + 1 < n)
                {
                    dBefore += StrokeSegmentOps.DistSq(chains[j][^1].P2, chains[j + 1][0].P1);
                    dAfter += StrokeSegmentOps.DistSq(chains[i + 1][0].P1, chains[j + 1][0].P1);
                }
                if (dAfter < dBefore - 1e-14)
                {
                    ReverseChainSubPath(chains, i + 1, j);
                    improved = true;
                }
            }
        }
    }

    private static void ReverseChainSubPath(List<List<StrokeSegment>> chains, int from, int to)
    {
        while (from < to)
        {
            var tmp = chains[from];
            chains[from] = ReverseChain(chains[to]);
            chains[to] = ReverseChain(tmp);
            from++;
            to--;
        }
        if (from == to)
            chains[from] = ReverseChain(chains[from]);
    }

    private static List<StrokeSegment> ReverseChain(List<StrokeSegment> chain)
    {
        var rev = new List<StrokeSegment>(chain.Count);
        for (var k = chain.Count - 1; k >= 0; k--)
            rev.Add(chain[k].Reverse());
        return rev;
    }
}
