using CadProjector.Core.Primitives;
using CadProjector.Rendering;
using System;
using System.Collections.Generic;

namespace CadProjector.Devices.Modules.Mesh
{
    public class ProjectorMesh : DeviceModule
 {
        public override string Name => "ProjectorMesh";
      public override string Description => "Calibration grid for projector";

        private int columns = 10;
    public int Columns
        {
            get => columns;
            set
       {
                if (value > 0 && columns != value)
    {
           columns = value;
           OnPropertyChanged();
        Update(this);
         }
       }
        }

        private int rows = 10;
        public int Rows
        {
    get => rows;
       set
      {
     if (value > 0 && rows != value)
   {
     rows = value;
   OnPropertyChanged();
    Update(this);
          }
            }
        }

  private double stepX = 0.1;
        public double StepX
      {
            get => stepX;
      set
     {
           if (value > 0 && stepX != value)
     {
             stepX = value;
         OnPropertyChanged();
       Update(this);
      }
   }
        }

        private double stepY = 0.1;
        public double StepY
        {
            get => stepY;
      set
 {
   if (value > 0 && stepY != value)
 {
              stepY = value;
    OnPropertyChanged();
    Update(this);
            }
   }
        }

        public LinesCollection GenerateMesh()
        {
   if (!IsEnabled) return new LinesCollection();

     var lines = new List<VectorLine>();
      
            // Generate horizontal lines
            for (int i = 0; i <= Rows; i++)
 {
              float y = i * (float)StepY;
       lines.Add(new VectorLine(
        new RenderPoint(0, y),
       new RenderPoint(Columns * StepX, y)));
       }

          // Generate vertical lines
       for (int i = 0; i <= Columns; i++)
    {
       float x = i * (float)StepX;
      lines.Add(new VectorLine(
             new RenderPoint(x, 0),
    new RenderPoint(x, Rows * StepY)));
        }

       return new LinesCollection(lines);
     }

        public LinesCollection GenerateCalibrationPoints(double radius = 0.01)
        {
   if (!IsEnabled) return new LinesCollection();

            var lines = new List<VectorLine>();
       
            for (int i = 0; i <= Rows; i++)
            {
  for (int j = 0; j <= Columns; j++)
    {
     float x = j * (float)StepX;
   float y = i * (float)StepY;
    
         // Generate small cross at each intersection
        lines.Add(new VectorLine(
      new RenderPoint(x - radius, y),
            new RenderPoint(x + radius, y)));
            lines.Add(new VectorLine(
       new RenderPoint(x, y - radius),
        new RenderPoint(x, y + radius)));
                }
          }

            return new LinesCollection(lines);
      }
    }
}