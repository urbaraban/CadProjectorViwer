using System;
using System.Collections.Generic;
using System.Linq;
using CadProjector.Core.Primitives;
using CadProjector.Rendering;

namespace CadProjector.Devices.Modules.Optimization
{
    public class FindShortestPath : DeviceModule
    {
        public override string Name => "ShortestPath";
        public override string Description => "Path optimization to reduce rendering time";

        private double maxAngle = 60;
        public double MaxAngle
        {
            get => maxAngle;
            set
            {
                maxAngle = Math.Max(0, Math.Min(180, value));
                Update(this);
            }
        }

        public LinesCollection Optimize(IEnumerable<VectorLine> lines)
        {
            if (!IsEnabled || lines == null)
                return new LinesCollection(lines ?? Array.Empty<VectorLine>());

            var result = new List<VectorLine>();
            var remainingLines = new List<VectorLine>(lines);

            if (remainingLines.Count == 0)
                return new LinesCollection();

            // Start with the first line
            var currentLine = remainingLines[0];
            remainingLines.RemoveAt(0);
            result.Add(currentLine);

            while (remainingLines.Count > 0)
            {
                var bestNextLine = FindBestNextLine(currentLine, remainingLines);
                if (bestNextLine.line != null)
                {
                    currentLine = bestNextLine.line;
                    remainingLines.Remove(currentLine);

                    if (bestNextLine.shouldReverse)
                        currentLine = currentLine.Reverse();

                    result.Add(currentLine);
                }
                else
                {
                    // If no suitable line found, take the first remaining one
                    currentLine = remainingLines[0];
                    remainingLines.RemoveAt(0);
                    result.Add(currentLine);
                }
            }

            return new LinesCollection(result);
        }

        private (VectorLine line, bool shouldReverse) FindBestNextLine(VectorLine currentLine, List<VectorLine> candidates)
        {
            VectorLine? bestLineNullable = null;
            bool bestShouldReverse = false;
            var minDistance = double.MaxValue;

            foreach (var candidate in candidates)
            {
                // Check distance from current end to candidate start
                var distanceToStart = GetDistance(currentLine.P2, candidate.P1);
                if (distanceToStart < minDistance)
                {
                    var angle = VectorLine.MinAngleBetween2D(currentLine.Vector, candidate.Vector);
                    if (angle * (180 / Math.PI) <= MaxAngle)
                    {
                        minDistance = distanceToStart;
                        bestLineNullable = candidate;
                        bestShouldReverse = false;
                    }
                }

                // Check distance from current end to candidate end (reversed)
                var distanceToEnd = GetDistance(currentLine.P2, candidate.P2);
                if (distanceToEnd < minDistance)
                {
                    var angle = VectorLine.MinAngleBetween2D(currentLine.Vector, -candidate.Vector);
                    if (angle * (180 / Math.PI) <= MaxAngle)
                    {
                        minDistance = distanceToEnd;
                        bestLineNullable = candidate;
                        bestShouldReverse = true;
                    }
                }
            }

            // Явное приведение типа: если bestLineNullable не null, возвращаем его, иначе возвращаем (null, false)
            return (bestLineNullable ?? default(VectorLine), bestShouldReverse);
        }

        private static double GetDistance(RenderPoint p1, RenderPoint p2)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}