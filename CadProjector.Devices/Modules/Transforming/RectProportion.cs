using CadProjector.Core.Primitives;
using CadProjector.Rendering;
using System;

namespace CadProjector.Devices.Modules.Transforming
{
    public class RectProportion : DeviceModule
    {
        public override string Name => "RectProportion";
        public override string Description => "Transform projection area dimensions";

        private double width = 1.0;
        public double Width
        {
            get => width;
            set
            {
                if (width != value)
                {
                    width = Math.Max(0.001, value);
                    OnPropertyChanged();
                    Update(this);
                }
            }
        }

        private double height = 1.0;
        public double Height
        {
            get => height;
            set
            {
                if (height != value)
                {
                    height = Math.Max(0.001, value);
                    OnPropertyChanged();
                    Update(this);
                }
            }
        }

        private double offsetX;
        public double OffsetX
        {
            get => offsetX;
            set
            {
                if (offsetX != value)
                {
                    offsetX = value;
                    OnPropertyChanged();
                    Update(this);
                }
            }
        }

        private double offsetY;
        public double OffsetY
        {
            get => offsetY;
            set
            {
                if (offsetY != value)
                {
                    offsetY = value;
                    OnPropertyChanged();
                    Update(this);
                }
            }
        }

        public RenderPoint Transform(RenderPoint point)
        {
            if (!IsEnabled) return point;

            return new RenderPoint(
             (point.X * Width) + OffsetX,
         (point.Y * Height) + OffsetY,
          point.Z
             );
        }

        public VectorLine Transform(VectorLine line)
        {
            if (!IsEnabled) return line;

            var p1 = Transform(line.P1);
            return new VectorLine(
                p1,
                new System.Numerics.Vector3(
                (float)(line.Vector.X * Width),
                (float)(line.Vector.Y * Height),
                line.Vector.Z
                       ),
            line.Length * Math.Max(Width, Height),
                line.IsBlank)
            {
                T1 = line.T1,
                T2 = line.T2
            };
        }

        public LinesCollection Transform(LinesCollection lines)
        {
            if (!IsEnabled) return lines;

            var transformedLines = new LinesCollection();
            foreach (var line in lines)
            {
                transformedLines.Add(Transform(line));
            }
            transformedLines.IsClosed = lines.IsClosed;
            return transformedLines;
        }
    }
}