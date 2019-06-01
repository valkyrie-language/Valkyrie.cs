namespace Valhalla;

/// <summary>
/// 包搜索结果
/// </summary>
public class PackageSearchResult
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}