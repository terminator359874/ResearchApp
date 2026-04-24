using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RecipeSearchApp.Models;

namespace RecipeSearchApp.Services
{
    public class InMemoryCache
    {
        private readonly Dictionary<int, CacheEntry> _cache = new();
        private readonly int _maxSize;
        private readonly TimeSpan _expirationTime;
        private int _totalMisses;
        private int _totalEvictions;
        private double _totalAccessTimeMs;

        public InMemoryCache(int maxSize = 100, int expirationMinutes = 30)
        {
            _maxSize = maxSize;
            _expirationTime = TimeSpan.FromMinutes(expirationMinutes);
        }

        public void Set(int key, Recipe value)
        {
            if (_cache.Count >= _maxSize && !_cache.ContainsKey(key))
            {
                var oldest = _cache.OrderBy(x => x.Value.LastAccessTime).First();
                _cache.Remove(oldest.Key);
                _totalEvictions++;
            }

            _cache[key] = new CacheEntry
            {
                Value = value,
                CreatedTime = DateTime.Now,
                LastAccessTime = DateTime.Now
            };
        }

        public Recipe? Get(int key)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (!_cache.TryGetValue(key, out var entry))
            {
                _totalMisses++;
                sw.Stop();
                _totalAccessTimeMs += sw.Elapsed.TotalMilliseconds;
                return null;
            }

            if (DateTime.Now - entry.CreatedTime > _expirationTime)
            {
                _cache.Remove(key);
                _totalEvictions++;
                _totalMisses++;
                sw.Stop();
                _totalAccessTimeMs += sw.Elapsed.TotalMilliseconds;
                return null;
            }

            entry.LastAccessTime = DateTime.Now;
            entry.HitCount++;

            sw.Stop();
            _totalAccessTimeMs += sw.Elapsed.TotalMilliseconds;

            return entry.Value;
        }

        public void Remove(int key) => _cache.Remove(key);

        public void Clear() => _cache.Clear();

        public Dictionary<string, object> GetStatistics()
        {
            var totalHits = _cache.Values.Sum(x => x.HitCount);
            var totalRequests = totalHits + _totalMisses;
            var hitRate = totalRequests > 0 ? (double)totalHits / totalRequests : 0;
            var missRate = totalRequests > 0 ? (double)_totalMisses / totalRequests : 0;
            var avgAccessTimeMs = totalRequests > 0 ? _totalAccessTimeMs / totalRequests : 0;

            var topRecipes = _cache.Values
                .OrderByDescending(x => x.HitCount)
                .Take(5)
                .Select(x => $"{x.Value.Title} (Hits: {x.HitCount})")
                .ToList();

            return new Dictionary<string, object>
            {
                { "CachedItems", _cache.Count },
                { "TotalHits", totalHits },
                { "TotalMisses", _totalMisses },
                { "TotalEvictions", _totalEvictions },
                { "MaxSize", _maxSize },
                { "ExpirationMinutes", _expirationTime.TotalMinutes },
                { "HitRate", hitRate },
                { "MissRate", missRate },
                { "AverageAccessTimeMs", avgAccessTimeMs },
                { "TopItems", string.Join("\n", topRecipes) }
            };
        }

        private class CacheEntry
        {
            public required Recipe Value { get; set; }
            public DateTime CreatedTime { get; set; }
            public DateTime LastAccessTime { get; set; }
            public int HitCount { get; set; }
        }
    }
}
