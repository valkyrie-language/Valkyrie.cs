# API 参考

## Valkyrie.Runtime

```csharp
namespace Valkyrie.Runtime;

public class ValkyrieRuntime
{
    /// <summary>
    /// 编译源码为 NyarVM 模块（.nyar 字节码）
    /// </summary>
    public CompileResult Compile(string sourceCode);

    /// <summary>
    /// 编译源码为 WASM 模块
    /// </summary>
    public CompileResult CompileToWasm(string sourceCode);

    /// <summary>
    /// 加载编译结果到运行时
    /// </summary>
    public ModuleId LoadModule(CompileResult result);

    /// <summary>
    /// 从 .nyar 字节码加载模块
    /// </summary>
    public ModuleId LoadBytecode(byte[] bytes);

    /// <summary>
    /// 运行指定模块中的函数
    /// </summary>
    public Value Run(ModuleId moduleId, string functionName, Value[] args);

    /// <summary>
    /// 注册领域方言
    /// </summary>
    public void RegisterDialect(IDialect dialect);
}

public class CompileResult
{
    public bool Success { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public EGraph<IKun> EGraph { get; }
    public ModuleId ModuleId { get; }
    public Target Target { get; }
    public byte[] OutputBytes { get; }
}
```

## Valkyrie.TypeChecker

```csharp
namespace Valkyrie.TypeChecker;

public class TypeChecker
{
    public void Check(CompilationUnit ast);

    public IReadOnlyList<TypeDiagnostic> Diagnostics { get; }
}

public class TypeDiagnostic
{
    public DiagnosticSeverity Severity { get; }
    public string Message { get; }
    public TextSpan Span { get; }
}

public enum DiagnosticSeverity
{
    Error,
    Warning,
    Info
}
```

## Valkyrie.PackageManager

```csharp
namespace Valkyrie.PackageManager;

public interface IRegistry
{
    string Name { get; }
    string Endpoint { get; set; }
    Task<Package> GetPackageAsync(string name, string version);
    Task<List<Package>> SearchPackagesAsync(string query);
    Task<PublishResult> PublishPackageAsync(PublishOptions opts, byte[] data);
    Task<string> DownloadPackageAsync(Package pkg, string dir);
    Task<List<string>> GetPackageVersionsAsync(string name);
    Task<TokenVerifyResult> VerifyTokenAsync(string token);
}
```

## Valkyrie.Formatter

```csharp
namespace Valkyrie.Formatter;

public class CodeFormatter
{
    public string Format(string sourceCode, FormatterConfig config);
    public string FormatFile(string filePath, FormatterConfig config);
}

public class FormatterConfig
{
    public IndentStyle IndentStyle { get; set; } = IndentStyle.Space;
    public int IndentSize { get; set; } = 4;
    public int LineWidth { get; set; } = 120;
    public BraceStyle BraceStyle { get; set; } = BraceStyle.NextLine;
    // ...
}
```

## Valkyrie.ToolChains

```csharp
namespace Valkyrie.ToolChains;

public static class ToolChainEntry
{
    /// <summary>
    /// 工具链统一入口，按命令分派到子工具
    /// </summary>
    public static Task<int> RunAsync(string[] args);
}
```

## Oak.Valkyrie（非 Valkyrie 项目，但相关）

```csharp
namespace Oak.Valkyrie;

public static class ValkyLexer
{
    public static IEnumerable<Token> Tokenize(string source);
}

public static class ValkyrieParser
{
    public static CompilationUnit Parse(IEnumerable<Token> tokens);
}
```
