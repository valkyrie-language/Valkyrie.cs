using Nyar.IR.EGraph;
using Nyar.IR.Intent;

namespace Valkyrie.Compiler.Mir;

/// <summary>
/// 中层中间表示模块。
/// `Graph` 承载可优化的 `EGraph<IKun>`，`Root` 指向模块根等价类。
/// </summary>
public sealed class MirModule
{
    public MirModule(string name, EGraph<IKun> graph, Id? root = null)
    {
        Name = name;
        Graph = graph;
        Root = root;
    }

    public string Name { get; }

    public EGraph<IKun> Graph { get; }

    public Id? Root { get; }
}
