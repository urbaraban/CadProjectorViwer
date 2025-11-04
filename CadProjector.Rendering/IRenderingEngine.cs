using CadProjector.Core.Objects;
using System;
using System.Collections.Generic;

namespace CadProjector.Rendering
{
    public interface IRenderingEngine
    {
        event EventHandler<IUidObject> RenderObjectAdded;
    event EventHandler<IUidObject> RenderObjectRemoved;
        
        IEnumerable<IUidObject> RenderObjects { get; }
  void Add(IUidObject obj);
        void Remove(IUidObject obj);
        void Clear();
        
   void InvalidateRender();
  }

    public interface IRenderObject
    {
      LinesCollection GetRenderLines();
        bool IsVisible { get; }
      double Opacity { get; }
    }
}