using Valkyrie.Compiler.Pipeline;
using Valkyrie.Compiler.Targets;
using Nyar.Assembler;

namespace Valkyrie.Compiler.Packaging;

/// <summary>
/// target packaging 抽象。
/// </summary>
public interface ITargetPackager
{
    ArtifactSet Package(
        string moduleName,
        OutputSpec generated,
        TargetContract targetContract);
}
