using Nyar.Assembler;

namespace Valkyrie.Interpreter.Incremental;

/// <summary>
/// 缓存的编译模块
/// </summary>
public sealed class CachedModule
{
    /// <summary>
    /// 源代码 SHA-256 哈希
    /// </summary>
    public string SourceHash { get; }

    /// <summary>
    /// 编译后的 CompilationUnit
    /// </summary>
    public CompilationUnit CompilationUnit { get; }

    /// <summary>
    /// 模块依赖列表
    /// </summary>
    public List<string> Dependencies { get; }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName { get; }

    public CachedModule(
        string sourceHash,
        CompilationUnit compilationUnit,
        List<string> dependencies,
        string moduleName)
    {
        SourceHash = sourceHash;
        CompilationUnit = compilationUnit;
        Dependencies = dependencies;
        ModuleName = moduleName;
    }
}
