using Nyar.Assembler;
using Nyar.Types;
using Nyar.VM;
using Nyar.VM.Bytecode;
using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Valkyrie.AST;
using Valkyrie.TypeChecker;

using AstCompilationUnit = Oak.Valkyrie.AST.CompilationUnit;
using AsmCompilationUnit = Nyar.Assembler.CompilationUnit;
namespace Valkyrie.Interpreter;

/// <summary>
/// Valkyrie 语言运行时门面，仅负责 NyarVM 装载与执行。
/// 编译与打包职责已迁移到 Valkyrie.Compiler。
/// </summary>
public sealed class ValkyrieRuntime
{
    private readonly DiagnosticSink _diagnostics;
    private readonly NyarVM _vm;

    public DiagnosticSink Diagnostics => _diagnostics;

    public ValkyrieRuntime()
    {
        _diagnostics = new DiagnosticSink();
        _vm = new NyarVM();
    }

    #region 运行时执行 API

    /// <summary>
    /// 加载 .nyar 字节码到运行时。
    /// </summary>
    public void LoadBytecode(byte[] bytecode)
    {
        _vm.Load(bytecode);
    }

    /// <summary>
    /// 加载 Nyar 模块到运行时。
    /// </summary>
    public void LoadModule(NyarModule module)
    {
        _vm.Load(module);
    }

    /// <summary>
    /// 加载 Nyar Standard IR（CompilationUnit）到运行时。
    /// </summary>
    public void LoadModule(AsmCompilationUnit unit)
    {
        var module = CompilationUnitSerializer.Serialize(unit);
        _vm.Load(module);
    }

    /// <summary>
    /// 执行指定模块函数。
    /// </summary>
    public Nyar.Value Run(string moduleName, string functionName, params Nyar.Value[] args)
    {
        return _vm.Run(moduleName, functionName, args);
    }

    #endregion

}
