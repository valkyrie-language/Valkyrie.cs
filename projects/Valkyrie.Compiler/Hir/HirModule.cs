using Nyar.Semantic;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Declaration;

namespace Valkyrie.Compiler.Hir;

/// <summary>
/// 高层中间表示模块。
/// 当前承载“已解析 AST + SemanticModel + 最小函数级语义”。
/// </summary>
public sealed class HirModule
{
    public HirModule(
        string name,
        CompilationUnit syntax,
        SemanticModel semantics,
        IReadOnlyList<HirFunction> functions)
    {
        Name = name;
        Syntax = syntax;
        Semantics = semantics;
        Functions = functions;
    }

    public string Name { get; }

    public CompilationUnit Syntax { get; }

    public SemanticModel Semantics { get; }

    public IReadOnlyList<HirFunction> Functions { get; }
}

/// <summary>
/// HIR 函数定义。
/// </summary>
public sealed class HirFunction
{
    public HirFunction(
        string name,
        MicroDeclaration syntax,
        IReadOnlyList<HirSymbolRef> parameters,
        HirTypeRef returnType,
        bool isLogicalEntry)
    {
        Name = name;
        Syntax = syntax;
        Parameters = parameters;
        ReturnType = returnType;
        IsLogicalEntry = isLogicalEntry;
    }

    public string Name { get; }

    public MicroDeclaration Syntax { get; }

    public IReadOnlyList<HirSymbolRef> Parameters { get; }

    public HirTypeRef ReturnType { get; }

    public bool IsLogicalEntry { get; }
}

/// <summary>
/// HIR 符号引用。
/// </summary>
public sealed record HirSymbolRef(string Name, HirTypeRef Type);

/// <summary>
/// HIR 类型引用。
/// </summary>
public sealed record HirTypeRef(string Name);
