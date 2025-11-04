using CadProjector.Core.Primitives;
using CadProjector.Rendering;
using System;

namespace CadProjector.Devices.Modules.Transforming
{
    public class Normalization : DeviceModule
    {
        public override string Name => "Normalization";
        public override string Description => "Normalize coordinates to fit projection area";

        private double maxSize = 1.0;
      public double MaxSize
        {
     get => maxSize;
            set
       {
      if (maxSize != value && value > 0)
        {
 maxSize = value;
    OnPropertyChanged();
       Update(this);
    }
       }
  }

        private bool autoScale = true;
        public bool AutoScale
        {
     get => autoScale;
 set
   {
       if (autoScale != value)
                {
       autoScale = value;
         OnPropertyChanged();
          Update(this);
    }
     }
        }

        public RenderPoint Transform(RenderPoint point)
        {
            if (!IsEnabled) return point;

  return new RenderPoint(
        point.X / MaxSize,
     point.Y / MaxSize,
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
         line.Vector.X,
        line.Vector.Y,
          line.Vector.Z
   ),
       line.Length / MaxSize,
           line.IsBlank)
            {
      T1 = line.T1,
          T2 = line.T2
};
        }

        public LinesCollection Transform(LinesCollection lines)
        {
            if (!IsEnabled || lines == null) 
       return lines;

            if (AutoScale)
         {
      // Find bounds
           double maxX = double.MinValue;
 double maxY = double.MinValue;
       double minX = double.MaxValue;
      double minY = double.MaxValue;

         foreach (var line in lines)
           {
         // Check P1
      maxX = Math.Max(maxX, line.P1.X);
           maxY = Math.Max(maxY, line.P1.Y);
        minX = Math.Min(minX, line.P1.X);
      minY = Math.Min(minY, line.P1.Y);

   // Check P2
         maxX = Math.Max(maxX, line.P2.X);
           maxY = Math.Max(maxY, line.P2.Y);
       minX = Math.Min(minX, line.P2.X);
    minY = Math.Min(minY, line.P2.Y);
}

      double width = maxX - minX;
       double height = maxY - minY;
         MaxSize = Math.Max(width, height);
     }

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