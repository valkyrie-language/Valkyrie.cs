namespace Asgard.CLI.Compiler;

/// <summary>
///     Island 声明
/// </summary>
public sealed class AwslIslandDecl
{
    public string ComponentName { get; set; } = string.Empty;
    public AwslIslandKind Kind { get; set; }
    public AwslHydrationStrategy Hydration { get; set; } = AwslHydrationStrategy.Eager;
}