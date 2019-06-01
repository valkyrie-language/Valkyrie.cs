namespace Legion.Version;

internal class VersionRequest
{
    public string RawSpec { get; set; } = string.Empty;
    public string ResolvedVersion { get; set; } = string.Empty;
}