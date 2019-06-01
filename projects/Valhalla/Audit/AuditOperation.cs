namespace Valhalla.Audit;

/// <summary>
/// 审计日志操作类型
/// </summary>
public enum AuditOperation
{
    RegisterOrg,
    RegisterPackage,
    Publish,
    Authorize,
    RevokeAuthorization,
    TransferPublisher,
    Shield,
    Unshield,
    Purge
}