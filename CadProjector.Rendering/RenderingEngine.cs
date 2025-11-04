using CadProjector.Core.Objects;
using CadProjector.Rendering.Cache;
using CadProjector.Rendering.Threading;
using CadProjector.Rendering.Transforms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace CadProjector.Rendering
{
    public class RenderingEngine : IRenderingEngine
    {
        private readonly ObservableCollection<IUidObject> objects = new();
        private readonly RenderCache renderCache = new();
        private readonly RenderTaskScheduler taskScheduler;
        private readonly ConcurrentDictionary<Guid, LinesCollection> processingCache = new();
        private bool isCacheDirty = true;

        public IEnumerable<IUidObject> RenderObjects => objects;

        public event EventHandler<IUidObject> RenderObjectAdded;
        public event EventHandler<IUidObject> RenderObjectRemoved;
        public event EventHandler RenderInvalidated;

        public RenderingEngine(int? maxThreads = null)
        {
            taskScheduler = new RenderTaskScheduler(maxThreads);
        }

        public void Add(IUidObject obj)
        {
            if (obj == null || objects.Contains(obj)) return;

            objects.Add(obj);
            if (obj is INotifyPropertyChanged notifyObj)
            {
                notifyObj.PropertyChanged += Object_PropertyChanged;
            }
            isCacheDirty = true;
            RenderObjectAdded?.Invoke(this, obj);
            InvalidateRender();

            // Start processing geometry in background
            if (obj is IRenderObject renderObj)
            {
                taskScheduler.EnqueueTask(new GeometryProcessingTask(obj, processingCache));
            }
        }

        public void Remove(IUidObject obj)
        {
            if (obj == null || !objects.Contains(obj)) return;

            if (obj is INotifyPropertyChanged notifyObj)
            {
                notifyObj.PropertyChanged -= Object_PropertyChanged;
            }
            objects.Remove(obj);
            renderCache.InvalidateGeometry(obj);
            processingCache.TryRemove(obj.Uid, out _);
            isCacheDirty = true;
            RenderObjectRemoved?.Invoke(this, obj);
            InvalidateRender();
        }

        public void Clear()
        {
            foreach (var obj in objects.OfType<INotifyPropertyChanged>())
            {
                obj.PropertyChanged -= Object_PropertyChanged;
            }
            objects.Clear();
            renderCache.Clear();
            processingCache.Clear();
            isCacheDirty = true;
            InvalidateRender();
        }

        public async Task<LinesCollection> GetCombinedFrameAsync(IEnumerable<ITransform> transforms = null)
        {
            var result = new ConcurrentDictionary<Guid, LinesCollection>();
            var tasks = new List<Task>();

            foreach (var obj in objects.OfType<IRenderObject>())
            {
                if (!obj.IsVisible) continue;

                var uidObj = obj as IUidObject;
                if (uidObj == null) continue;

                // Исправлено: передаем uidObj вместо obj
                if (!isCacheDirty && renderCache.TryGetFrame(uidObj, out var cachedFrame))
                {
                    result.TryAdd(uidObj.Uid, cachedFrame);
                    continue;
                }

                if (processingCache.TryGetValue(uidObj.Uid, out var processedGeometry))
                {
                    if (transforms != null && transforms.Any())
                    {
                        taskScheduler.EnqueueTask(new TransformProcessingTask(
                            processedGeometry,
                            transforms,
                            result,
                            uidObj.Uid));
                    }
                    else
                    {
                        result.TryAdd(uidObj.Uid, processedGeometry);
                    }
                }
            }

            await Task.WhenAll(tasks);

            var allLines = result.Values.SelectMany(lines => lines).ToList();
            var combinedFrame = new LinesCollection(allLines);

            isCacheDirty = false;
            return combinedFrame;
        }

        private void Object_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is IUidObject obj)
            {
                renderCache.InvalidateGeometry(obj);
                processingCache.TryRemove(obj.Uid, out _);
                if (obj is IRenderObject renderObj)
                {
                    taskScheduler.EnqueueTask(new GeometryProcessingTask(obj, processingCache));
                }
                isCacheDirty = true;
                InvalidateRender();
            }
        }

        public void InvalidateRender()
        {
            RenderInvalidated?.Invoke(this, EventArgs.Empty);
        }
    }
}