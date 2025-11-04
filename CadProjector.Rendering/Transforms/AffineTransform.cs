using CadProjector.Core.Primitives;
using System;
using System.Numerics;

namespace CadProjector.Rendering.Transforms
{
    public class AffineTransform : ITransform
    {
        private Matrix4x4 transformMatrix;

        public AffineTransform()
        {
            transformMatrix = Matrix4x4.Identity;
        }

        public void Translate(float x, float y, float z = 0)
        {
            transformMatrix *= Matrix4x4.CreateTranslation(x, y, z);
        }

        public void Scale(float x, float y, float z = 1)
        {
            transformMatrix *= Matrix4x4.CreateScale(x, y, z);
        }

        public void Rotate(float angleInDegrees)
        {
            float angleInRadians = angleInDegrees * (float)(Math.PI / 180.0);
            transformMatrix *= Matrix4x4.CreateRotationZ(angleInRadians);
        }

        public RenderPoint Transform(RenderPoint point)
        {
            Vector3 vector = new Vector3(point.X, point.Y, point.Z);
            vector = Vector3.Transform(vector, transformMatrix);
            return new RenderPoint(vector.X, vector.Y, vector.Z);
        }

        public Vector3 Transform(Vector3 vector)
        {
            return Vector3.Transform(vector, transformMatrix);
        }

        public void Reset()
        {
            transformMatrix = Matrix4x4.Identity;
        }
    }
}