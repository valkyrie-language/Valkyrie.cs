namespace Valkyrie.CLI.Compiler;

/// <summary>
/// VCC 构建结果
/// </summary>
public sealed class VccBuildResult
{
    /// <summary>
    /// 构建是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// 输出目录
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 生成的输出文件列表
    /// </summary>
    public List<string> OutputFiles { get; set; } = [];

    /// <summary>
    /// 主产物路径
    /// </summary>
    public string MainArtifact { get; set; } = string.Empty;
}
