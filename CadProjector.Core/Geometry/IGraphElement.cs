namespace CadProjector.Core.Geometry
{
 public interface IGraphElement
    {
 bool IsNaN { get; }
        IGraphElement Reverse();
    }
}