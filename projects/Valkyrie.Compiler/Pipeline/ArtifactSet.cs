namespace Valkyrie.Compiler.Pipeline;

/// <summary>
/// 统一交付结果。
/// </summary>
public sealed class ArtifactSet
{
    public ArtifactSet(
        CompilerArtifact primaryArtifact,
        IReadOnlyList<CompilerArtifact>? sidecarArtifacts = null,
        IReadOnlyList<CompilerArtifact>? debugArtifacts = null,
        RunContract? runContract = null)
    {
        PrimaryArtifact = primaryArtifact;
        SidecarArtifacts = sidecarArtifacts ?? [];
        DebugArtifacts = debugArtifacts ?? [];
        RunContract = runContract;
    }

    public CompilerArtifact PrimaryArtifact { get; }

    public IReadOnlyList<CompilerArtifact> SidecarArtifacts { get; }

    public IReadOnlyList<CompilerArtifact> DebugArtifacts { get; }

    public RunContract? RunContract { get; }
}

/// <summary>
/// 编译产物项。
/// </summary>
public sealed record CompilerArtifact(string Name, byte[] Content, string MediaType);

/// <summary>
/// 宿主运行契约。
/// </summary>
public sealed record RunContract(
    string LogicalEntry,
    string PhysicalEntry,
    string InvocationShape,
    string ValidationCommand);
