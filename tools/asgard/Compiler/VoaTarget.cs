namespace Asgard.CLI.Compiler;

/// <summary>
///     VOA 编译目标平台定义
/// </summary>
public sealed class VoaTarget
{
    /// <summary>目标平台 ID（wasm / wasi / clr / jvm / native）</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>目标平台可读名称</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>指令集架构</summary>
    public string Arch { get; init; } = string.Empty;

    /// <summary>目标三元组 / ABI 标识</summary>
    public string Abi { get; init; } = string.Empty;

    /// <summary>输出文件格式（wasm / dll / class / exe）</summary>
    public string OutputFormat { get; init; } = string.Empty;

    /// <summary>是否需要 JS glue 代码</summary>
    public bool HasJsGlue { get; init; }

    /// <summary>是否支持 PWA 生成</summary>
    public bool HasPwa { get; init; }

    /// <summary>目标描述</summary>
    public string Description { get; init; } = string.Empty;
}