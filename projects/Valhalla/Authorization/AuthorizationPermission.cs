namespace Valhalla.Authorization;

/// <summary>
/// 授权权限枚举
/// </summary>
[Flags]
public enum AuthorizationPermission
{
    /// <summary>发布版本</summary>
    Publish = 1,
    /// <summary>委托下级命名空间授权</summary>
    Delegate = 2,
    /// <summary>完全控制</summary>
    Full = Publish | Delegate
}