using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.ECS;
using Valkyrie.Linter.Rules;

namespace Valkyrie.Linter;

/// <summary>
/// Lint 主引擎
/// 编排所有 Lint 规则，支持规则配置和抑制
/// </summary>
public sealed class LinterEngine
{
    private readonly List<ILintRule> _rules;
    private readonly Dictionary<string, LintLevel> _levelOverrides;
    private readonly HashSet<string> _suppressedRules;

    /// <summary>
    /// 创建 Linter 引擎，加载默认规则集
    /// </summary>
    public LinterEngine()
    {
        _rules =
        [
            new CodeQualityRules(),
            new EcsRules(),
            new SecurityRules()
        ];
        _levelOverrides = new Dictionary<string, LintLevel>(StringComparer.Ordinal);
        _suppressedRules = new HashSet<string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// 创建 Linter 引擎，使用自定义规则集
    /// </summary>
    public LinterEngine(IEnumerable<ILintRule> rules)
    {
        _rules = [.. rules];
        _levelOverrides = new Dictionary<string, LintLevel>(StringComparer.Ordinal);
        _suppressedRules = new HashSet<string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// 已注册的规则列表
    /// </summary>
    public IReadOnlyList<ILintRule> Rules => _rules;

    /// <summary>
    /// 覆盖规则的严重级别
    /// </summary>
    public void SetLevel(string ruleId, LintLevel level)
    {
        if (level == LintLevel.Off)
        {
            _suppressedRules.Add(ruleId);
            _levelOverrides.Remove(ruleId);
        }
        else
        {
            _levelOverrides[ruleId] = level;
            _suppressedRules.Remove(ruleId);
        }
    }

    /// <summary>
    /// 抑制指定规则
    /// </summary>
    public void Suppress(string ruleId)
    {
        _suppressedRules.Add(ruleId);
    }

    /// <summary>
    /// 启用指定规则
    /// </summary>
    public void Enable(string ruleId)
    {
        _suppressedRules.Remove(ruleId);
    }

    /// <summary>
    /// 添加自定义规则
    /// </summary>
    public void AddRule(ILintRule rule)
    {
        _rules.Add(rule);
    }

    /// <summary>
    /// 对 AST 执行所有 Lint 规则检查
    /// </summary>
    public LintResult Check(CompilationUnit ast, string? filePath = null)
    {
        var allDiagnostics = new List<LintDiagnostic>();

        var allowList = CollectAllowAttributes(ast);

        foreach (var rule in _rules)
        {
            if (_suppressedRules.Contains(rule.RuleId))
            {
                continue;
            }

            var diagnostics = rule.Check(ast, filePath);

            foreach (var diag in diagnostics)
            {
                if (allowList.Contains(diag.RuleId))
                {
                    continue;
                }

                var effectiveLevel = ResolveLevel(diag.RuleId, diag.Level);

                if (effectiveLevel == LintLevel.Off)
                {
                    continue;
                }

                allDiagnostics.Add(diag with { Level = effectiveLevel });
            }
        }

        return new LintResult { Diagnostics = allDiagnostics };
    }

    /// <summary>
    /// 解析规则的有效严重级别
    /// </summary>
    private LintLevel ResolveLevel(string ruleId, LintLevel defaultLevel)
    {
        return _levelOverrides.GetValueOrDefault(ruleId, defaultLevel);
    }

    /// <summary>
    /// 收集 AST 中的 #[allow(...)] 抑制属性
    /// </summary>
    private static HashSet<string> CollectAllowAttributes(CompilationUnit ast)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var decl in ast.Declarations)
        {
            if (decl is AttributeDecl attr && attr.Name == "allow")
            {
                foreach (var arg in attr.Arguments)
                {
                    var ruleId = arg.Value?.ToString()?.Trim('"', '\'');
                    if (ruleId is not null)
                    {
                        allowed.Add(ruleId);
                    }
                }
            }

            foreach (var declAttr in GetDeclAttributes(decl))
            {
                if (declAttr.Name == "allow")
                {
                    foreach (var arg in declAttr.Arguments)
                    {
                        var ruleId = arg.Value?.ToString()?.Trim('"', '\'');
                        if (ruleId is not null)
                        {
                            allowed.Add(ruleId);
                        }
                    }
                }
            }
        }

        return allowed;
    }

    private static IReadOnlyList<AttributeDecl> GetDeclAttributes(AstNode decl)
    {
        return decl switch
        {
            FunctionDecl f => f.Attributes,
            VariableDecl v => v.Attributes,
            StructureDecl s => s.Attributes,
            ClassDecl c => c.Attributes,
            ComponentDecl c => c.Attributes,
            SystemDecl s => s.Attributes,
            _ => []
        };
    }
}