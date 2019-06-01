using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.Types;

namespace Valkyrie.Tests.E2ETests;

file sealed class TestCostModel2 : ICostModel
{
    public CostVector NodeCost(IKun node)
    {
        return node switch
        {
            IKun.Constant or IKun.FloatConstant or IKun.BooleanConstant
                or IKun.StringConstant or IKun.None => CostVector.FromLatency(1),
            IKun.Symbol => CostVector.FromLatency(1),
            IKun.BinaryOp => CostVector.FromLatency(2),
            IKun.UnaryOp => CostVector.FromLatency(2),
            IKun.Lambda => CostVector.FromLatency(3),
            IKun.Apply => CostVector.FromLatency(5),
            IKun.Choice => CostVector.FromLatency(3),
            IKun.Seq => CostVector.FromLatency(1),
            IKun.StateUpdate => CostVector.FromLatency(2),
            IKun.Return => CostVector.FromLatency(1),
            IKun.Extension => CostVector.FromLatency(5),
            _ => CostVector.FromLatency(3)
        };
    }

    public int Compare(CostVector a, CostVector b)
    {
        return a.CompareTo(b);
    }
}