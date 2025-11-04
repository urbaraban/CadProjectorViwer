using CadProjector.Core.Primitives;
using CadProjector.Rendering;

namespace CadProjector.Devices.Modules.Transforming
{
    public class FrameTransformer : DeviceModule
    {
        public override string Name => "FrameTransformer";
        public override string Description => "Transform and combine frames";

        private bool optimizeOrder = true;
        public bool OptimizeOrder
        {
            get => optimizeOrder;
            set
            {
                if (optimizeOrder != value)
                {
                    optimizeOrder = value;
                    OnPropertyChanged();
                    Update(this);
                }
            }
        }

        private bool removeBlankLines = false;
        public bool RemoveBlankLines
        {
            get => removeBlankLines;
            set
            {
                if (removeBlankLines != value)
                {
                    removeBlankLines = value;
                    OnPropertyChanged();
                    Update(this);
                }
            }
        }

        public LinesCollection Transform(IEnumerable<LinesCollection> frames)
        {
            if (!IsEnabled) return new LinesCollection();

            var allLines = new List<VectorLine>();

            foreach (var frame in frames)
            {
                if (RemoveBlankLines)
                {
                    allLines.AddRange(frame.Where(l => !l.IsBlank));
                }
                else
                {
                    allLines.AddRange(frame);
                }
            }

            if (OptimizeOrder && allLines.Count > 0)
            {
                var optimized = new List<VectorLine>();
                var current = allLines[0];
                allLines.RemoveAt(0);
                optimized.Add(current);

                while (allLines.Count > 0)
                {
                    var nearest = FindNearestLine(current, allLines);
                    if (nearest.shouldReverse)
                    {
                        nearest.line = nearest.line.Reverse();
                    }
                    optimized.Add(nearest.line);
                    allLines.Remove(nearest.line);
                    current = nearest.line;
                }

                return new LinesCollection(optimized);
            }

            return new LinesCollection(allLines);
        }

        private (VectorLine line, bool shouldReverse) FindNearestLine(VectorLine current, List<VectorLine> candidates)
        {
            var bestMatch = (line: candidates[0], shouldReverse: false);
            var minDistance = double.MaxValue;

            foreach (var candidate in candidates)
            {
                // Check distance from current end to candidate start
                var distanceToStart = GetDistance(current.P2, candidate.P1);
                if (distanceToStart < minDistance)
                {
                    minDistance = distanceToStart;
                    bestMatch = (candidate, false);
                }

                // Check distance from current end to candidate end (reversed)
                var distanceToEnd = GetDistance(current.P2, candidate.P2);
                if (distanceToEnd < minDistance)
                {
                    minDistance = distanceToEnd;
                    bestMatch = (candidate, true);
                }
            }

            return bestMatch;
        }

        private static double GetDistance(RenderPoint p1, RenderPoint p2)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}