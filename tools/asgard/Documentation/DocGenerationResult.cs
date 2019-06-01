namespace Asgard.CLI.Documentation;

/// <summary>
///     文档生成结果
/// </summary>
public sealed class DocGenerationResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>输出文件路径</summary>
    public string? OutputPath { get; set; }

    /// <summary>处理的文件数</summary>
    public int FilesProcessed { get; set; }

    /// <summary>提取的函数数</summary>
    public int FunctionsFound { get; set; }

    /// <summary>提取的结构体数</summary>
    public int StructsFound { get; set; }

    /// <summary>提取的枚举数</summary>
    public int EnumsFound { get; set; }

    /// <summary>提取的外函数数</summary>
    public int ExternsFound { get; set; }
}