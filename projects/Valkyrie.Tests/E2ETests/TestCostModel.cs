using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.Types;

namespace Valkyrie.Tests.E2ETests;

file sealed class TestCostModel : ICostModel
{
    public CostVector NodeCost(IKun node)
    {
        return new CostVector(1, 1, 1, 1);
    }

    public int Compare(CostVector a, CostVector b)
    {
        return a.CompareTo(b);
    }
}