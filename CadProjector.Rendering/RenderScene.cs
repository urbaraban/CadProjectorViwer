using CadProjector.Core.Objects;
using CadProjector.Core.Primitives;
using System;
using System.Collections.Generic;
using CadProjector.Rendering.Transforms; // Добавлено для доступа к ITransform из нужного пространства имён

namespace CadProjector.Rendering
{
    public class RenderScene
    {
        private readonly RenderingEngine renderingEngine;
        private readonly List<CadProjector.Rendering.Transforms.ITransform> transforms = new(); // Исправлено: используем правильный тип

        public RenderScene()
        {
            renderingEngine = new RenderingEngine();
        }

        public IEnumerable<IUidObject> Objects => renderingEngine.RenderObjects;

        public event EventHandler<IUidObject> ObjectAdded
        {
            add { renderingEngine.RenderObjectAdded += value; }
            remove { renderingEngine.RenderObjectAdded -= value; }
        }

        public event EventHandler<IUidObject> ObjectRemoved
        {
            add { renderingEngine.RenderObjectRemoved += value; }
            remove { renderingEngine.RenderObjectRemoved -= value; }
        }

        public void AddTransform(CadProjector.Rendering.Transforms.ITransform transform) // Исправлено: используем правильный тип
        {
            if (transform != null && !transforms.Contains(transform))
            {
                transforms.Add(transform);
            }
        }

        public void RemoveTransform(CadProjector.Rendering.Transforms.ITransform transform) // Исправлено: используем правильный тип
        {
            if (transform != null)
            {
                transforms.Remove(transform);
            }
        }

        public void Add(IUidObject obj)
        {
            renderingEngine.Add(obj);
        }

        public void Remove(IUidObject obj)
        {
            renderingEngine.Remove(obj);
        }

        public void Clear()
        {
            renderingEngine.Clear();
        }

        public LinesCollection GetFrame()
        {
            // Исправлено: используем асинхронный метод GetCombinedFrameAsync и ожидаем результат
            var frameTask = renderingEngine.GetCombinedFrameAsync(transforms);
            frameTask.Wait();
            var frame = frameTask.Result;

            // Apply all transforms
            foreach (var transform in transforms)
            {
                for (int i = 0; i < frame.Count; i++)
                {
                    var line = frame[i];
                    var p1 = transform.Transform(line.P1);
                    frame[i] = new VectorLine(
                        p1,
                        transform.Transform(line.Vector),
                        line.Length,
                        line.IsBlank)
                    {
                        T1 = line.T1,
                        T2 = line.T2
                    };
                }
            }

            return frame;
        }

        // Удалено внутреннее определение интерфейса ITransform, чтобы избежать конфликта типов
    }
}