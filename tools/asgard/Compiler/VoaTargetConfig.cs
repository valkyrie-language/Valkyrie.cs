namespace Asgard.CLI.Compiler;

/// <summary>
///     VOA 多目标编译配置
///     定义 VOA 框架可编译到的所有目标平台及其特性
/// </summary>
public sealed class VoaTargetConfig
{
    /// <summary>
    ///     所有支持的目标平台列表
    /// </summary>
    public static readonly IReadOnlyList<VoaTarget> SupportedTargets = new[]
    {
        new VoaTarget
        {
            Id = "wasm",
            Name = "WebAssembly (Browser)",
            Arch = "wasm32",
            Abi = "wasm32-unknown-unknown",
            OutputFormat = "wasm",
            HasJsGlue = true,
            HasPwa = true,
            Description = "浏览器端 WebAssembly，配套 JS glue + HTML 模板",
        },
        new VoaTarget
        {
            Id = "wasi",
            Name = "WebAssembly + WASI",
            Arch = "wasm32",
            Abi = "wasm32-wasi-preview1",
            OutputFormat = "wasm",
            HasJsGlue = false,
            HasPwa = false,
            Description = "WASI 运行时（wasmtime / WasmEdge / Node.js WASI），_start 入口",
        },
        new VoaTarget
        {
            Id = "clr",
            Name = ".NET CLR (MSIL)",
            Arch = "il",
            Abi = "net9.0",
            OutputFormat = "dll",
            HasJsGlue = false,
            HasPwa = false,
            Description = "CLR MSIL 跨平台程序集，通过 Nyar Assembler/CLR/ 后端发射",
        },
        new VoaTarget
        {
            Id = "jvm",
            Name = "JVM (Java Bytecode)",
            Arch = "jvm",
            Abi = "jvm21",
            OutputFormat = "class",
            HasJsGlue = false,
            HasPwa = false,
            Description = "JVM 字节码，通过 Nyar Assembler/JVM/ + Acorn.Jvm 编码",
        },
        new VoaTarget
        {
            Id = "native",
            Name = "Native (AOT)",
            Arch = "x86_64",
            Abi = "native",
            OutputFormat = "exe",
            HasJsGlue = false,
            HasPwa = false,
            Description = "Native AOT 编译，通过 Nyar Assembler/Native/ 后端 + LLVM 代码生成",
        },
    };

    /// <summary>
    ///     根据 ID 查找目标平台
    /// </summary>
    public static VoaTarget? FindTarget(string targetId)
    {
        foreach (var target in SupportedTargets)
        {
            if (target.Id == targetId.ToLowerInvariant())
            {
                return target;
            }
        }

        return null;
    }
}