using System.Collections.Concurrent;

namespace Asgard.CLI.DevServer;

/// <summary>
///     VOA Fetch 内存缓存 — TTL 过期 + 标签反向索引 + 容量限制，仅供 DevServer/SSR 使用
/// </summary>
public sealed class VoaFetchCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _tagIndex = new();
    private readonly int _maxEntries;
    private long _hits;
    private long _misses;
    private long _evictions;

    public VoaFetchCache(int maxEntries = 1000)
    {
        _maxEntries = maxEntries;
    }

    public int Count => _entries.Count;
    public long Hits => _hits;
    public long Misses => _misses;
    public long Evictions => _evictions;

    public bool TryGet(string cacheKey, out FetchResponse response)
    {
        if (_entries.TryGetValue(cacheKey, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                Interlocked.Increment(ref _hits);
                response = entry.Response;
                return true;
            }

            _entries.TryRemove(cacheKey, out _);
            RemoveFromTagIndex(cacheKey, entry.Tags);
        }

        Interlocked.Increment(ref _misses);
        response = default!;
        return false;
    }

    public void Set(string cacheKey, FetchResponse response, int ttlSeconds, List<string>? tags = null)
    {
        while (_entries.Count >= _maxEntries)
        {
            var oldest = _entries.OrderBy(kvp => kvp.Value.CreatedAt).FirstOrDefault();
            if (oldest.Key is null) break;

            if (_entries.TryRemove(oldest.Key, out var oldEntry))
            {
                RemoveFromTagIndex(oldest.Key, oldEntry.Tags);
                Interlocked.Increment(ref _evictions);
            }
        }

        var entry = new CacheEntry
        {
            Response = response,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds),
            Tags = tags ?? new List<string>()
        };

        _entries[cacheKey] = entry;

        foreach (var tag in entry.Tags)
        {
            _tagIndex.AddOrUpdate(tag,
                _ => new HashSet<string> { cacheKey },
                (_, set) =>
                {
                    set.Add(cacheKey);
                    return set;
                });
        }
    }

    public int InvalidateByTag(string tag)
    {
        if (!_tagIndex.TryGetValue(tag, out var keys))
        {
            return 0;
        }

        var count = 0;
        foreach (var key in keys)
        {
            if (_entries.TryRemove(key, out _))
            {
                count++;
            }
        }

        _tagIndex.TryRemove(tag, out _);
        return count;
    }

    public int InvalidateByPath(string pathPattern)
    {
        var count = 0;
        var keysToRemove = new List<string>();

        foreach (var kvp in _entries)
        {
            var url = kvp.Value.Response.Headers.TryGetValue("x-request-url", out var reqUrl) ? reqUrl : "";
            if (url.Contains(pathPattern, StringComparison.OrdinalIgnoreCase))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            if (_entries.TryRemove(key, out var entry))
            {
                RemoveFromTagIndex(key, entry.Tags);
                count++;
            }
        }

        return count;
    }

    public void Clear()
    {
        _entries.Clear();
        _tagIndex.Clear();
        _hits = 0;
        _misses = 0;
        _evictions = 0;
    }

    public CacheStats GetStats()
    {
        return new CacheStats
        {
            Entries = _entries.Count,
            Hits = _hits,
            Misses = _misses,
            Evictions = _evictions
        };
    }

    private void RemoveFromTagIndex(string cacheKey, List<string> tags)
    {
        foreach (var tag in tags)
        {
            if (_tagIndex.TryGetValue(tag, out var keys))
            {
                keys.Remove(cacheKey);
                if (keys.Count == 0)
                {
                    _tagIndex.TryRemove(tag, out _);
                }
            }
        }
    }

    private sealed class CacheEntry
    {
        public required FetchResponse Response { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime ExpiresAt { get; set; }
        public List<string> Tags { get; init; } = new();
    }
}

public sealed class CacheStats
{
    public int Entries { get; set; }
    public long Hits { get; set; }
    public long Misses { get; set; }
    public long Evictions { get; set; }
}
