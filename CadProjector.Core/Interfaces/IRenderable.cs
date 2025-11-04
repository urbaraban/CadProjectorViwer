using System;
using System.Collections.Generic;

namespace CadProjector.Core.Interfaces
{
    // Добавьте определение интерфейса IRenderingDisplay, чтобы устранить ошибку CS0246.
    public interface IRenderingDisplay
    {
        // Определите необходимые члены интерфейса здесь, если они нужны.
    }

    public interface IRenderable
    {
        Guid Uid { get; }
        IEnumerable<IRenderedObject> GetRender(IRenderingDisplay display);
    }

    public interface IRenderedObject
    {
    }
}