namespace Valkyrie.Compiler.Targets;

/// <summary>
/// `CanonicalTriple` 到目标契约的注册表。
/// 当前先提供最小解析骨架，后续补充完整契约表。
/// </summary>
public sealed class CanonicalTripleRegistry
{
    public TargetContract Resolve(string canonicalTriple)
    {
        return canonicalTriple switch
        {
            "nyarvm-standard" => new TargetContract(canonicalTriple, "NyarVM", "nyarvm", "bytecode", "Module"),
            "wasm32-unknown-browser" => new TargetContract(canonicalTriple, "WASM", "browser", "webassembly", "Module"),
            "wasm32-unknown-node" => new TargetContract(canonicalTriple, "WASM", "node", "webassembly", "Module"),
            "wasm32-unknown-deno" => new TargetContract(canonicalTriple, "WASM", "deno", "webassembly", "Module"),
            "wasm32-unknown-bun" => new TargetContract(canonicalTriple, "WASM", "bun", "webassembly", "Module"),
            "wasm32-unknown-wasi-wasip1" => new TargetContract(canonicalTriple, "WASM", "wasi-p1", "wasip1", "Executable"),
            "wasm32-unknown-wasi-wasip2" => new TargetContract(canonicalTriple, "WASM", "wasi-p2", "wasip2", "Component"),
            "jvm-openjdk-linux" => new TargetContract(canonicalTriple, "JVM", "jdk", "jvm", "Executable"),
            "clr-microsoft-windows" => new TargetContract(canonicalTriple, "CLR", "dotnet", "clr", "Executable"),
            _ => throw new NotSupportedException($"暂不支持的 CanonicalTriple：{canonicalTriple}")
        };
    }
}

/// <summary>
/// 围绕 `CanonicalTriple` 的最小目标契约。
/// </summary>
public sealed record TargetContract(
    string CanonicalTriple,
    string BackendFamily,
    string HostKind,
    string AbiProfile,
    string OutputKind);
