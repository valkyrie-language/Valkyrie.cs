namespace Asgard.CLI.Compiler;

/// <summary>
///     AWSL → GGScript 中间表示（IR）的顶层编译单元
/// </summary>
public sealed class AwslCompilationUnit
{
    public List<AwslComponentDecl> Components { get; set; } = [];
    public AwslConfigDecl? Config { get; set; }
    public AwslRouteManifest? Routes { get; set; }
}