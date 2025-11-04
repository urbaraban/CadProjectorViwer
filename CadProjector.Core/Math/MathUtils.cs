using CadProjector.Core.Primitives;

namespace CadProjector.Core.Math
{
    public static class MathUtils
    {
        public static double GetLength(RenderPoint p1, RenderPoint p2)
        {
            return System.Math.Sqrt(
                System.Math.Pow(p2.X - p1.X, 2) +
                System.Math.Pow(p2.Y - p1.Y, 2) +
                System.Math.Pow(p2.Z - p1.Z, 2));
        }

        public static double GetLength2D(RenderPoint p1, RenderPoint p2)
        {
            return System.Math.Sqrt(
                System.Math.Pow(p2.X - p1.X, 2) +
                System.Math.Pow(p2.Y - p1.Y, 2));
        }

        public static double GetLength2D(double x1, double y1, double x2, double y2)
        {
            return System.Math.Sqrt(
                System.Math.Pow(x2 - x1, 2) +
                System.Math.Pow(y2 - y1, 2));
        }
    }
}