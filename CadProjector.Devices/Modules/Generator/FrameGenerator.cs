using CadProjector.Core.Primitives;
using CadProjector.Rendering;
using System;
using System.Collections.Generic;

namespace CadProjector.Devices.Modules.Generator
{
    public class FrameGenerator : DeviceModule
    {
        public override string Name => "FrameGenerator";
        public override string Description => "Generate frame shapes";

      private int segments = 32;
        public int Segments
        {
          get => segments;
  set
            {
     if (segments != value && value > 2)
  {
     segments = value;
           OnPropertyChanged();
         Update(this);
     }
    }
        }

        public LinesCollection GenerateCircle(double radius = 1, bool isClosed = true)
{
            var points = new List<RenderPoint>();
        double angleStep = 2 * Math.PI / Segments;

            for (int i = 0; i <= Segments; i++)
         {
          double angle = i * angleStep;
            points.Add(new RenderPoint(
     radius * Math.Cos(angle),
          radius * Math.Sin(angle)));
     }

        var lines = new LinesCollection();
   for (int i = 0; i < points.Count - 1; i++)
            {
                lines.Add(new VectorLine(points[i], points[i + 1]));
            }
lines.IsClosed = isClosed;

            return lines;
        }

        public LinesCollection GenerateRectangle(double width, double height, bool isClosed = true)
{
    var points = new List<RenderPoint>
   {
         new RenderPoint(-width/2, -height/2),
    new RenderPoint(width/2, -height/2),
       new RenderPoint(width/2, height/2),
          new RenderPoint(-width/2, height/2),
            new RenderPoint(-width/2, -height/2)
            };

            var lines = new LinesCollection();
         for (int i = 0; i < points.Count - 1; i++)
          {
        lines.Add(new VectorLine(points[i], points[i + 1]));
            }
  lines.IsClosed = isClosed;

      return lines;
        }

        public LinesCollection GeneratePolygon(int sides, double radius = 1, bool isClosed = true)
        {
  if (sides < 3) sides = 3;
        var points = new List<RenderPoint>();
       double angleStep = 2 * Math.PI / sides;

            for (int i = 0; i <= sides; i++)
{
    double angle = i * angleStep;
           points.Add(new RenderPoint(
        radius * Math.Cos(angle),
        radius * Math.Sin(angle)));
            }

    var lines = new LinesCollection();
        for (int i = 0; i < points.Count - 1; i++)
 {
      lines.Add(new VectorLine(points[i], points[i + 1]));
            }
            lines.IsClosed = isClosed;

       return lines;
      }

        public LinesCollection GenerateStar(int points = 5, double outerRadius = 1, double innerRadius = 0.5, bool isClosed = true)
        {
if (points < 3) points = 3;
        var vertices = new List<RenderPoint>();
     double angleStep = Math.PI / points;

            for (int i = 0; i <= 2 * points; i++)
       {
            double angle = i * angleStep;
           double radius = i % 2 == 0 ? outerRadius : innerRadius;
    vertices.Add(new RenderPoint(
    radius * Math.Cos(angle),
          radius * Math.Sin(angle)));
  }

  var lines = new LinesCollection();
  for (int i = 0; i < vertices.Count - 1; i++)
    {
      lines.Add(new VectorLine(vertices[i], vertices[i + 1]));
          }
    lines.IsClosed = isClosed;

            return lines;
        }
    }
}