using CadProjector.Core.Objects;

namespace CadProjector.Rendering.Cache
{
    internal class RenderCache
    {
        private readonly Dictionary<Guid, CachedGeometry> geometryCache = new();
        private readonly Dictionary<Guid, LinesCollection> frameCache = new();

        public void CacheGeometry(IUidObject obj, LinesCollection geometry)
        {
            if (obj == null) return;

            geometryCache[obj.Uid] = new CachedGeometry
            {
                Geometry = geometry,
                LastUpdate = DateTime.Now
            };
        }

        public void CacheFrame(IUidObject obj, LinesCollection frame)
        {
            if (obj == null) return;
            frameCache[obj.Uid] = frame;
        }

        public bool TryGetGeometry(IUidObject obj, out LinesCollection geometry)
        {
            geometry = null;
            if (obj == null) return false;

            if (geometryCache.TryGetValue(obj.Uid, out var cached))
            {
                geometry = cached.Geometry;
                return true;
            }
            return false;
        }

        public bool TryGetFrame(IUidObject obj, out LinesCollection frame)
        {
            return frameCache.TryGetValue(obj.Uid, out frame);
        }

        public void InvalidateGeometry(IUidObject obj)
        {
            if (obj == null) return;
            geometryCache.Remove(obj.Uid);
            frameCache.Remove(obj.Uid);
        }

        public void Clear()
        {
            geometryCache.Clear();
            frameCache.Clear();
        }

        private class CachedGeometry
        {
            public LinesCollection Geometry { get; set; }
            public DateTime LastUpdate { get; set; }
        }
    }
}