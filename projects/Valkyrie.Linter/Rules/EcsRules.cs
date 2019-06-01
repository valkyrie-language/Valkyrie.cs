using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.ECS;

namespace Valkyrie.Linter.Rules;

/// <summary>
/// ECS 架构约束规则（VALK_E001 - VALK_E005）
/// </summary>
public sealed class EcsRules : ILintRule
{
    public string RuleId => "VALK_E";
    public string Description => "ECS 架构约束规则集合";
    public LintLevel DefaultLevel => LintLevel.Error;
    public LintCategory Category => LintCategory.Ecs;

    public IReadOnlyList<LintDiagnostic> Check(CompilationUnit ast, string? filePath = null)
    {
        var diagnostics = new List<LintDiagnostic>();

        CollectComponentWithLogic(ast, filePath, diagnostics);
        CollectSystemWithState(ast, filePath, diagnostics);
        CollectUnusedQueries(ast, filePath, diagnostics);
        CollectMissingLifecycle(ast, filePath, diagnostics);
        CollectCircularDependencies(ast, filePath, diagnostics);

        return diagnostics;
    }

    #region VALK_E001 组件包含逻辑

    private void CollectComponentWithLogic(CompilationUnit ast, string? filePath, List<LintDiagnostic> diagnostics)
    {
        foreach (var decl in ast.Declarations)
        {
            if (decl is ComponentDecl component)
            {
                foreach (var field in component.Fields)
                {
                    if (field.FieldType.Name is "fn" or "Function" or "closure" or "Closure")
                    {
                        diagnostics.Add(new LintDiagnostic
                        {
                            RuleId = LintRuleIds.ComponentWithLogic,
                            Message = $"组件 '{component.Name}' 包含函数类型字段 '{field.Name}'，组件应只包含数据",
                            Level = LintLevel.Error,
                            Span = field.Span,
                            Suggestion = "将逻辑移至 System 中，组件只保留数据字段",
                            FilePath = filePath
                        });
                    }
                }
            }
        }
    }

    #endregion

    #region VALK_E002 系统包含状态

    private void CollectSystemWithState(CompilationUnit ast, string? filePath, List<LintDiagnostic> diagnostics)
    {
        foreach (var decl in ast.Declarations)
        {
            if (decl is SystemDecl system)
            {
                foreach (var method in system.Methods)
                {
                    if (method.Body is not null)
                    {
                        var hasLocalState = HasVariableDeclarations(method.Body);
                        if (hasLocalState)
                        {
                            diagnostics.Add(new LintDiagnostic
                            {
                                RuleId = LintRuleIds.SystemWithState,
                                Message = $"系统 '{system.Name}' 的方法 '{method.Name}' 包含局部状态，系统应无状态",
                                Level = LintLevel.Warning,
                                Span = method.Span,
                                Suggestion = "将状态移至 Component 中，系统只通过 Query 访问数据",
                                FilePath = filePath
                            });
                        }
                    }
                }
            }
        }
    }

    private static bool HasVariableDeclarations(BlockStmt block)
    {
        foreach (var stmt in block.Statements)
        {
            if (stmt is VariableDecl { IsMutable: true })
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region VALK_E003 查询未使用

    private void CollectUnusedQueries(CompilationUnit ast, string? filePath, List<LintDiagnostic> diagnostics)
    {
        foreach (var decl in ast.Declarations)
        {
            if (decl is SystemDecl system)
            {
                var usedNames = new HashSet<string>(StringComparer.Ordinal);

                foreach (var method in system.Methods)
                {
                    if (method.Body is not null)
                    {
                        CollectUsedNamesInBlock(method.Body, usedNames);
                    }
                }

                foreach (var query in system.Queries)
                {
                    var queryDesc = query.ComponentTypes.Count > 0
                        ? string.Join("+", query.ComponentTypes.Select(c => c.Name))
                        : query.Kind.ToString();

                    var isUsed = query.ComponentTypes.Any(ct => usedNames.Contains(ct.Name));

                    if (!isUsed && query.ComponentTypes.Count > 0)
                    {
                        diagnostics.Add(new LintDiagnostic
                        {
                            RuleId = LintRuleIds.UnusedQuery,
                            Message = $"系统 '{system.Name}' 中的查询（{queryDesc}）可能未使用",
                            Level = LintLevel.Warning,
                            Span = query.Span,
                            Suggestion = "删除未使用的查询",
                            FilePath = filePath
                        });
                    }
                }
            }
        }
    }

    private static void CollectUsedNamesInBlock(BlockStmt block, HashSet<string> names)
    {
        foreach (var stmt in block.Statements)
        {
            CollectUsedNamesInNode(stmt, names);
        }
    }

    private static void CollectUsedNamesInNode(AstNode node, HashSet<string> names)
    {
        if (node is IdentifierNode ident)
        {
            names.Add(ident.Name);
        }

        if (node is BlockStmt block)
        {
            foreach (var stmt in block.Statements)
            {
                CollectUsedNamesInNode(stmt, names);
            }
        }
    }

    #endregion

    #region VALK_E004 System 缺少生命周期

    private void CollectMissingLifecycle(CompilationUnit ast, string? filePath, List<LintDiagnostic> diagnostics)
    {
        foreach (var decl in ast.Declarations)
        {
            if (decl is SystemDecl system)
            {
                var hasLifecycle = system.Attributes.Any(a =>
                    a.Name is "on_update" or "on_init" or "on_render" or "on_fixed_update" or "on_late_update");

                if (!hasLifecycle)
                {
                    diagnostics.Add(new LintDiagnostic
                    {
                        RuleId = LintRuleIds.MissingLifecycle,
                        Message = $"系统 '{system.Name}' 缺少生命周期标注",
                        Level = LintLevel.Info,
                        Span = system.Span,
                        Suggestion = "添加 #[on_update] / #[on_init] / #[on_render] 等生命周期标注",
                        FilePath = filePath
                    });
                }
            }
        }
    }

    #endregion

    #region VALK_E005 循环依赖 Component

    private void CollectCircularDependencies(CompilationUnit ast, string? filePath, List<LintDiagnostic> diagnostics)
    {
        var componentFields = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var decl in ast.Declarations)
        {
            if (decl is ComponentDecl component)
            {
                var referencedTypes = new HashSet<string>(StringComparer.Ordinal);
                foreach (var field in component.Fields)
                {
                    var typeName = field.FieldType.Name;
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        referencedTypes.Add(typeName);
                    }
                }
                componentFields[component.Name] = referencedTypes;
            }
        }

        foreach (var (componentName, references) in componentFields)
        {
            if (references.Contains(componentName))
            {
                diagnostics.Add(new LintDiagnostic
                {
                    RuleId = LintRuleIds.CircularComponentDependency,
                    Message = $"组件 '{componentName}' 包含对自身的引用，形成循环依赖",
                    Level = LintLevel.Error,
                    Span = default,
                    Suggestion = "使用 Entity 引用代替直接组件引用，或重构为父子关系",
                    FilePath = filePath
                });
            }
        }
    }

    #endregion
}