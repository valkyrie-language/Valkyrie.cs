using System.Text.Json;

namespace Asgard.CLI.Effect;

/// <summary>
///     Effect 副作用存储，管理 pending/resolved/rejected 条目的生命周期
/// </summary>
public sealed class EffectStore
{
    private readonly Dictionary<string, EffectEntry> _entries = new();
    private readonly Dictionary<string, long> _timestamps = new();

    /// <summary>等待中的 Effect 数量</summary>
    public int PendingCount => _entries.Values.Count(e => e.Status == EffectStatus.Pending);

    /// <summary>已解决的 Effect 数量</summary>
    public int ResolvedCount => _entries.Values.Count(e => e.Status == EffectStatus.Resolved);

    /// <summary>已拒绝的 Effect 数量</summary>
    public int RejectedCount => _entries.Values.Count(e => e.Status == EffectStatus.Rejected);

    /// <summary>
    ///     注册一个新的 Effect 条目
    /// </summary>
    /// <param name="functionName">副作用函数名</param>
    /// <param name="args">函数参数</param>
    /// <param name="config">副作用配置</param>
    /// <returns>Effect ID，失败返回 null</returns>
    public string? Register(string functionName, string[] args, EffectConfig config)
    {
        var id = $"ef-{Guid.NewGuid():N}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        _entries[id] = new EffectEntry
        {
            Id = id,
            FunctionName = functionName,
            Args = args,
            Status = EffectStatus.Pending,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        _timestamps[id] = timestamp;
        return id;
    }

    /// <summary>
    ///     解决一个 Effect 条目
    /// </summary>
    /// <param name="entryId">Effect ID</param>
    /// <param name="result">解决结果</param>
    public void Resolve(string entryId, JsonElement result)
    {
        if (!_entries.TryGetValue(entryId, out var entry))
        {
            return;
        }

        _entries[entryId] = entry with
        {
            Status = EffectStatus.Resolved,
            Result = result,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    ///     拒绝一个 Effect 条目
    /// </summary>
    /// <param name="entryId">Effect ID</param>
    /// <param name="error">拒绝原因</param>
    public void Reject(string entryId, string error)
    {
        if (!_entries.TryGetValue(entryId, out var entry))
        {
            return;
        }

        _entries[entryId] = entry with
        {
            Status = EffectStatus.Rejected,
            Error = error,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    ///     获取 Effect 结果
    /// </summary>
    /// <param name="entryId">Effect ID</param>
    /// <returns>Effect 结果，不存在返回 null</returns>
    public EffectResult? GetResult(string entryId)
    {
        if (!_entries.TryGetValue(entryId, out var entry))
        {
            return null;
        }

        return new EffectResult
        {
            Status = entry.Status,
            Data = entry.Result,
            Error = entry.Status == EffectStatus.Rejected ? entry.Error : null,
            EntryId = entryId
        };
    }

    /// <summary>
    ///     使超时的 Pending Effect 过期
    /// </summary>
    /// <param name="now">当前时间戳（毫秒）</param>
    public void Expire(long now)
    {
        foreach (var (id, timestamp) in _timestamps)
        {
            if (now - timestamp > 0 && _entries.TryGetValue(id, out var entry) && entry.Status == EffectStatus.Pending)
            {
                _entries[id] = entry with
                {
                    Status = EffectStatus.Rejected,
                    Error = "timeout",
                    UpdatedAt = now
                };
            }
        }
    }
}
