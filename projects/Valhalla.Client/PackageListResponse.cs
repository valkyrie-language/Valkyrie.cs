namespace Valhalla.Client;

/// <summary>
/// 包列表页响应
/// </summary>
public class PackageListResponse
{
    /// <summary>包列表</summary>
    public List<PackageSummary> Packages { get; set; } = new();

    /// <summary>总数</summary>
    public int Total { get; set; }

    /// <summary>当前页码</summary>
    public int Page { get; set; }

    /// <summary>每页大小</summary>
    public int Size { get; set; }
}