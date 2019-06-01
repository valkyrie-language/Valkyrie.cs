namespace Asgard.CLI.Compiler;

/// <summary>
///     路由清单
/// </summary>
public sealed class AwslRouteManifest
{
    public List<AwslRouteEntry> Entries { get; set; } = [];
}