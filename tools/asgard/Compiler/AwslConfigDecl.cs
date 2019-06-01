namespace Asgard.CLI.Compiler;

/// <summary>
///     全局配置
/// </summary>
public sealed class AwslConfigDecl
{
    public bool PwaEnabled { get; set; }
    public bool HmrEnabled { get; set; }
    public bool SsrEnabled { get; set; }
}