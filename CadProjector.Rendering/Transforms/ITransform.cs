using CadProjector.Core.Primitives;
using System.Numerics;

namespace CadProjector.Rendering.Transforms
{
    public interface ITransform
    {
      RenderPoint Transform(RenderPoint point);
        Vector3 Transform(Vector3 vector);
    }
}