namespace Legion.Dependency;

public class DependencyNode
{
    public string PackageName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string RegistryName { get; set; } = "npm";
    public List<DependencyNode> Dependencies { get; set; } = new();
    public string? TargetCondition { get; set; }
    public bool IsSdk { get; set; }
    public string? SdkModuleName { get; set; }
}