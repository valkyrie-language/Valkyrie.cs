using Nyar.Types;

namespace Valkyrie.Interpreter.Incremental;

/// <summary>
///     编译目标扩展
/// </summary>
public static class CompilationTargetExtensions
{
    /// <summary>
    ///     获取目标平台名称
    /// </summary>
    public static string GetPlatformName(this CompilationTarget target)
    {
        return target.Runtime switch
        {
            TargetRuntime.NyarVM => "nyarvm",
            TargetRuntime.Wasm => "wasm",
            TargetRuntime.Wasi => "wasip1",
            TargetRuntime.Jvm => "jvm",
            TargetRuntime.Clr => "clr",
            TargetRuntime.Native => "native",
            _ => "unknown"
        };
    }

    /// <summary>
    ///     是否支持并行编译
    /// </summary>
    public static bool SupportsParallelCompilation(this CompilationTarget target)
    {
        return target.Runtime switch
        {
            TargetRuntime.NyarVM => true,
            TargetRuntime.Wasm => true,
            TargetRuntime.Wasi => true,
            _ => false
        };
    }
}
