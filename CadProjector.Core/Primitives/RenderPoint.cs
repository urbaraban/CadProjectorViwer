namespace CadProjector.Core.Primitives
{
    public struct RenderPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public bool IsNaN => float.IsNaN(X) || float.IsNaN(Y) || float.IsNaN(Z);

        public RenderPoint(float x, float y, float z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public RenderPoint(double x, double y, double z = 0)
        : this((float)x, (float)y, (float)z)
        {
        }

        public RenderPoint Move(double deltaX, double deltaY)
        {
            return new RenderPoint(X + deltaX, Y + deltaY, Z);
        }
    }
}