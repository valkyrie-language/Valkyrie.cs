using System.Text.Json;

namespace Valhalla.Audit;

/// <summary>
/// 审计日志存储
/// </summary>
public class AuditLog
{
    private readonly string _auditFilePath;
    private readonly List<AuditEntry> _entries;

    public AuditLog(string auditDirectory)
    {
        _auditFilePath = Path.Combine(auditDirectory, "audit.log");
        _entries = LoadEntries();
    }

    /// <summary>
    ///     追加一条审计日志
    /// </summary>
    public void Append(AuditEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Actor))
        {
            throw new ArgumentException("Actor 不能为空");
        }

        entry.Timestamp = DateTime.UtcNow;
        _entries.Add(entry);
        Persist();
    }

    /// <summary>
    ///     记录包发布操作
    /// </summary>
    public void LogPublish(string packageName, string version, string publisher,
        string? sha256 = null, Dictionary<string, string>? details = null)
    {
        var entry = new AuditEntry
        {
            Operation = AuditOperation.Publish,
            PackageName = packageName,
            Version = version,
            Actor = publisher,
            Sha256 = sha256,
            Details = details ?? new Dictionary<string, string>()
        };
        Append(entry);
    }

    /// <summary>
    ///     记录组织注册操作
    /// </summary>
    public void LogRegisterOrg(string orgName, string actor, string role)
    {
        var entry = new AuditEntry
        {
            Operation = AuditOperation.RegisterOrg,
            PackageName = orgName,
            Actor = actor,
            ActorRole = role
        };
        Append(entry);
    }

    /// <summary>
    ///     记录包屏蔽操作
    /// </summary>
    public void LogShield(string packageName, string version, string actor,
        string reason)
    {
        var entry = new AuditEntry
        {
            Operation = AuditOperation.Shield,
            PackageName = packageName,
            Version = version,
            Actor = actor,
            Details = new Dictionary<string, string> { ["reason"] = reason }
        };
        Append(entry);
    }

    /// <summary>
    ///     记录授权操作
    /// </summary>
    public void LogAuthorize(string packageName, string actor, string authorizedActor, string role)
    {
        var entry = new AuditEntry
        {
            Operation = AuditOperation.Authorize,
            PackageName = packageName,
            Actor = actor,
            Details = new Dictionary<string, string>
            {
                ["authorized"] = authorizedActor,
                ["role"] = role
            }
        };
        Append(entry);
    }

    /// <summary>
    ///     记录取消授权操作
    /// </summary>
    public void LogRevokeAuthorization(string packageName, string actor, string revokedActor)
    {
        var entry = new AuditEntry
        {
            Operation = AuditOperation.RevokeAuthorization,
            PackageName = packageName,
            Actor = actor,
            Details = new Dictionary<string, string> { ["revoked"] = revokedActor }
        };
        Append(entry);
    }

    /// <summary>
    ///     记录转移发布者操作
    /// </summary>
    public void LogTransferPublisher(string packageName, string fromActor, string toActor)
    {
        var entry = new AuditEntry
        {
            Operation = AuditOperation.TransferPublisher,
            PackageName = packageName,
            Actor = fromActor,
            Details = new Dictionary<string, string> { ["to"] = toActor }
        };
        Append(entry);
    }

    /// <summary>
    ///     查询指定包的所有审计日志
    /// </summary>
    public List<AuditEntry> QueryByPackage(string packageName)
    {
        return _entries
            .Where(e => e.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    ///     查询指定操作的审计日志
    /// </summary>
    public List<AuditEntry> QueryByOperation(AuditOperation operation)
    {
        return _entries.Where(e => e.Operation == operation).ToList();
    }

    /// <summary>
    ///     查询指定时间范围内的审计日志
    /// </summary>
    public List<AuditEntry> QueryByTimeRange(DateTime from, DateTime to)
    {
        return _entries
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .ToList();
    }

    /// <summary>
    ///     获取所有审计日志
    /// </summary>
    public IReadOnlyList<AuditEntry> GetAll()
    {
        return _entries.AsReadOnly();
    }

    private List<AuditEntry> LoadEntries()
    {
        if (!File.Exists(_auditFilePath))
        {
            return new List<AuditEntry>();
        }

        try
        {
            var json = File.ReadAllText(_auditFilePath);
            return JsonSerializer.Deserialize<List<AuditEntry>>(json) ?? new List<AuditEntry>();
        }
        catch
        {
            return new List<AuditEntry>();
        }
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_auditFilePath, json);
    }
}