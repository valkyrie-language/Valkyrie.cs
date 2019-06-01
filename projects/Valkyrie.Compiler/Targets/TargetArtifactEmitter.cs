using Nyar.Assembler;
using Nyar.Types;
using Valkyrie.Compiler.Packaging;
using Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Compiler.Targets;

/// <summary>
/// 将 LIR/CgModule 发射为目标交付产物。
/// </summary>
public sealed class TargetArtifactEmitter
{
    private readonly BackendSelector _backendSelector;
    private readonly CanonicalTripleRegistry _canonicalTripleRegistry;
    private readonly ITargetPackager _packager;

    public TargetArtifactEmitter()
    {
        _backendSelector = new BackendSelector([]);
        _canonicalTripleRegistry = new CanonicalTripleRegistry();
        _packager = new DefaultTargetPackager();
    }

    public void RegisterBackend(ICodeGenBackend backend)
    {
        _backendSelector.RegisterBackend(backend);
    }

    public ArtifactSet Emit(CgModule module, CompilationTarget target, CompilationOptions? options = null)
    {
        var backend = _backendSelector.SelectBackend(target.Arch);
        if (backend is null)
        {
            throw new InvalidOperationException($"未找到目标后端：{target.Arch}");
        }

        if (!backend.Validate(module, out var diagnostics))
        {
            var reason = diagnostics.Count == 0
                ? "后端校验失败"
                : string.Join("; ", diagnostics.Select(diagnostic => diagnostic.Message));
            throw new InvalidOperationException(reason);
        }

        var effectiveOptions = options ?? new CompilationOptions();
        effectiveOptions.Target = target;
        var generated = backend.Compile(module, effectiveOptions);

        var canonicalTriple = ResolveCanonicalTriple(target);
        if (string.IsNullOrWhiteSpace(canonicalTriple))
        {
            throw new InvalidOperationException("无法为当前目标解析 CanonicalTriple。");
        }

        var targetContract = _canonicalTripleRegistry.Resolve(canonicalTriple);
        return _packager.Package(module.Name, generated, targetContract);
    }

    private static string? ResolveCanonicalTriple(CompilationTarget target)
    {
        return target.Arch switch
        {
            Arch.NyarVm => "nyarvm-standard",
            Arch.Jvm => "jvm-openjdk-linux",
            Arch.Clr => "clr-microsoft-windows",
            Arch.Wasm32 or Arch.Wasm64 when target.Abi == ABI.WasiP1 => "wasm32-unknown-wasi-wasip1",
            Arch.Wasm32 or Arch.Wasm64 when target.Abi == ABI.WasiP2 => "wasm32-unknown-wasi-wasip2",
            Arch.Wasm32 or Arch.Wasm64 => "wasm32-unknown-browser",
            _ => null
        };
    }
}
