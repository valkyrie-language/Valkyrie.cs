using Oak.Syntax;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.Term;

namespace Valkyrie.TypeChecker;

/// <summary>
/// 可达性分析和确定性赋值分/// </summary>
public partial class TypeChecker
{
    private readonly HashSet<string> _assignedLocals = new(StringComparer.Ordinal);
    private bool _afterReturn;
    private bool _afterDiscard;
    private bool _afterBreak;
    private bool _afterContinue;

    /// <summary>
    /// 重置块级分析状态（进入新语句块时调用）
    /// </summary>
    private void SaveAndResetBlockState()
    {
        _savedBlockStates.Push(new BlockState
        {
            AssignedLocals = new HashSet<string>(_assignedLocals, StringComparer.Ordinal),
            AfterReturn = _afterReturn,
            AfterBreak = _afterBreak,
            AfterContinue = _afterContinue
        });

        _afterReturn = false;
        _afterBreak = false;
        _afterContinue = false;
    }

    /// <summary>
    /// 恢复块级分析状态（离开语句块时调用    /// </summary>
    private void RestoreBlockState()
    {
        if (_savedBlockStates.TryPop(out var state))
        {
            _assignedLocals.IntersectWith(state.AssignedLocals);
            _afterReturn = state.AfterReturn;
            _afterBreak = state.AfterBreak;
            _afterContinue = state.AfterContinue;
        }
    }

    private readonly Stack<BlockState> _savedBlockStates = new();

    /// <summary>
    /// 检查语句或声明是否可达
    /// </summary>
    private void CheckReachable(AstNode node, string nodeLabel)
    {
        if (_afterReturn)
        {
            AddWarning("VALK2060", $"无法访问的代码：{nodeLabel} 位于 return 语句之后", node.Span);
            return;
        }

        if (_afterBreak)
        {
            AddWarning("VALK2061", $"无法访问的代码：{nodeLabel} 位于 break 语句之后", node.Span);
            return;
        }

        if (_afterContinue)
        {
            AddWarning("VALK2062", $"无法访问的代码：{nodeLabel} 位于 continue 语句之后", node.Span);
            return;
        }

        if (_afterDiscard)
        {
            AddWarning("VALK2063", $"无法访问的代码：{nodeLabel} 位于 discard 语句之后", node.Span);
        }
    }

    /// <summary>
    /// 标记当前块中后续代码不可达（return 调用    /// </summary>
    private void MarkReturn()
    {
        _afterReturn = true;
    }

    /// <summary>
    /// 标记当前块中后续代码不可达（discard 调用    /// </summary>
    private void MarkDiscard()
    {
        _afterDiscard = true;
    }

    /// <summary>
    /// 标记当前块中后续代码不可达（break 调用    /// </summary>
    private void MarkBreak()
    {
        _afterBreak = true;
    }

    /// <summary>
    /// 标记当前块中后续代码不可达（continue 调用    /// </summary>
    private void MarkContinue()
    {
        _afterContinue = true;
    }

    /// <summary>
    /// 记录变量已赋    /// </summary>
    private void MarkAssigned(string variableName)
    {
        _assignedLocals.Add(variableName);
    }

    /// <summary>
    /// 检查变量是否已赋值（如果未赋值且非参全局，报错）
    /// </summary>
    private void CheckDefinitelyAssigned(string variableName, TextSpan span)
    {
        if (!_assignedLocals.Contains(variableName))
        {
            var symbol = _currentScope.Resolve(variableName);
            if (symbol is not null && symbol.Kind == SymbolKind.Variable)
            {
                AddWarning("VALK2064", $"使用了可能未赋值的变量 '{variableName}'", span);
            }
        }
    }

    private struct BlockState
    {
        public HashSet<string> AssignedLocals;
        public bool AfterReturn;
        public bool AfterBreak;
        public bool AfterContinue;
    }
}
