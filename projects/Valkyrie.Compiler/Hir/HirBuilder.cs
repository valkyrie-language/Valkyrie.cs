using Nyar.Semantic;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Declaration;

namespace Valkyrie.Compiler.Hir;

/// <summary>
/// 将 Valkyrie AST 与 `SemanticModel` 组织为最小可用的 `HIR`。
/// </summary>
public sealed class HirBuilder
{
    public HirModule Build(CompilationUnit syntax, SemanticModel semantics, string moduleName)
    {
        var functions = syntax.Declarations
            .OfType<MicroDeclaration>()
            .Select(BuildFunction)
            .ToArray();

        return new HirModule(moduleName, syntax, semantics, functions);
    }

    private static HirFunction BuildFunction(MicroDeclaration declaration)
    {
        var parameters = declaration.Parameters
            .Select(parameter => new HirSymbolRef(
                parameter.Name,
                new HirTypeRef(parameter.ParamType?.Name ?? "unknown")))
            .ToArray();
        var returnType = new HirTypeRef(declaration.ReturnType?.Name ?? "void");
        var isLogicalEntry = declaration.Attributes.Any(attribute => attribute.Name == "main");

        return new HirFunction(
            declaration.Name,
            declaration,
            parameters,
            returnType,
            isLogicalEntry);
    }
}
