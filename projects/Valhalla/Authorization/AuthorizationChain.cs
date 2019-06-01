namespace Valhalla.Authorization;

/// <summary>
/// 授权链——从根组织到最终发布者的授权路径
/// </summary>
public class AuthorizationChain
{
    /// <summary>授权事件的顺序列表（从根到叶子）</summary>
    public List<AuthorizationGrant> Grants { get; set; } = new();

    /// <summary>整个链的 SHA-256，用于 lock 文件记录</summary>
    public string ChainHash { get; set; } = string.Empty;

    /// <summary>获取链的根授权者</summary>
    public string? RootIssuer => Grants.Count > 0 ? Grants[0].Issuer : null;

    /// <summary>获取链的最终被授权者</summary>
    public string? LeafGrantee => Grants.Count > 0 ? Grants[^1].Grantee : null;

    /// <summary>验证链中所有授权的连续性</summary>
    public bool IsChainContinuous()
    {
        for (int i = 1; i < Grants.Count; i++)
        {
            if (Grants[i].Issuer != Grants[i - 1].Grantee)
            {
                return false;
            }
        }

        return true;
    }
}