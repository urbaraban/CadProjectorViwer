using System;
using System.Numerics;
using CadProjector.Core.Primitives;
using CadProjector.Core.Geometry;
using CadProjector.Core.Math;

namespace CadProjector.Rendering
{
    public struct VectorLine : IGraphElement
    {
        public RenderPoint P1 { get; set; }
        public byte T1 { get; set; }
        public byte T2 { get; set; }
        public double Length { get; set; }
        public Vector3 Vector { get; set; }
        public bool IsBlank { get; set; }

        public bool IsNaN => P1.IsNaN;

        public RenderPoint P2 => new RenderPoint(
           P1.X + (float.IsNaN(Vector.X) ? 0 : Vector.X * (float)Length),
            P1.Y + (float.IsNaN(Vector.Y) ? 0 : Vector.Y * (float)Length),
             P1.Z + (float.IsNaN(Vector.Z) ? 0 : Vector.Z * (float)Length)
        );

        public VectorLine()
        {
            P1 = new RenderPoint();
            Length = float.NaN;
            Vector = new Vector3();
            IsBlank = false;
            T1 = T2 = 1;
        }

        public VectorLine(RenderPoint p1, RenderPoint p2, bool isBlank = false)
        {
            P1 = p1;
            Vector = Vector3.Normalize(new Vector3(
             p2.X - p1.X,
                 p2.Y - p1.Y,
               p2.Z - p1.Z));
            Length = MathUtils.GetLength(p1, p2);
            IsBlank = isBlank;
            T1 = T2 = 1;
        }

        public VectorLine(RenderPoint p1, Vector3 vector, double length, bool isBlank)
        {
            P1 = p1;
            Vector = vector;
            Length = length;
            IsBlank = isBlank;
            T1 = T2 = 1;
        }

        public VectorLine Reverse()
        {
            return new VectorLine
            {
                P1 = P2,
                Vector = Vector3.Negate(Vector),
                Length = Length,
                IsBlank = IsBlank,
                T1 = T2,
                T2 = T1
            };
        }

        IGraphElement IGraphElement.Reverse() => Reverse();

        public static RenderPoint operator *(VectorLine line, double length)
        {
            return new RenderPoint(
                line.P1.X + line.Vector.X * (float)length,
                line.P1.Y + line.Vector.Y * (float)length,
                line.P1.Z + line.Vector.Z * (float)length);
        }

        public override bool Equals(object obj)
        {
            return obj is VectorLine line &&
              P1.Equals(line.P1) &&
                 Vector.Equals(line.Vector) &&
                    Length.Equals(line.Length) &&
                   IsBlank == line.IsBlank;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(P1, Vector, Length, IsBlank);
        }

        public static double MinAngleBetween2D(Vector3 vector, Vector3 vector3)
        {
            // Реализация вычисления угла между двумя векторами в 2D
            double dot = vector.X * vector3.X + vector.Y * vector3.Y;
            double mag1 = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            double mag2 = Math.Sqrt(vector3.X * vector3.X + vector3.Y * vector3.Y);
            if (mag1 == 0 || mag2 == 0) return 0;
            double cos = dot / (mag1 * mag2);
            cos = Math.Clamp(cos, -1.0, 1.0);
            var angle = Math.Abs(Math.Acos(cos));
            return angle;
        }

        public static bool operator ==(VectorLine left, VectorLine right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VectorLine left, VectorLine right)
        {
            return !(left == right);
        }
    }
}