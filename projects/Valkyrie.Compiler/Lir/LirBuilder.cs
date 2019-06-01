using Nyar.Assembler;
using Nyar.IR.EGraph;
using Nyar.IR.Intent;
using Valkyrie.Compiler.Mir;

namespace Valkyrie.Compiler.Lir;

/// <summary>
/// 将最小 MIR（EGraph + IKun）降级为 `CgModule`。
/// </summary>
public sealed class LirBuilder
{
    #region 入口

    public LirModule Build(MirModule mir)
    {
        var module = new CgModule(mir.Name);
        if (!mir.Root.HasValue)
        {
            return new LirModule(module);
        }

        var rootNode = ResolveNode(mir.Graph, mir.Root.Value);
        if (rootNode is not IKun.Module moduleNode)
        {
            return new LirModule(module);
        }

        foreach (var memberId in moduleNode.Children)
        {
            if (ResolveNode(mir.Graph, memberId) is not IKun.Export exportNode)
            {
                continue;
            }

            if (ResolveNode(mir.Graph, exportNode.Value) is not IKun.Lambda lambdaNode)
            {
                continue;
            }

            var function = LowerFunction(mir.Graph, exportNode.Name, lambdaNode);
            var functionIndex = module.Functions.Count;
            module.AddFunction(function);
            module.AddExport(new CgModuleExport(exportNode.Name, CgExportKind.Function, functionIndex));
        }

        return new LirModule(module);
    }

    #endregion

    #region 函数降级

    private static CgFunction LowerFunction(EGraph<IKun> graph, string functionName, IKun.Lambda lambdaNode)
    {
        var context = new FunctionLoweringContext(graph, lambdaNode.Parameters, InferReturnType(graph, lambdaNode.Body));
        LowerStatement(lambdaNode.Body, context);
        EnsureReturn(context);

        var function = new CgFunction(functionName, ToCgTypeName(context.ReturnType));
        foreach (var parameterName in lambdaNode.Parameters)
        {
            function.AddParameter(parameterName, "i32");
        }

        foreach (var instruction in context.Instructions)
        {
            function.AddInstruction(instruction);
        }

        return function;
    }

    #endregion

    #region 语句与表达式

    private static void LowerStatement(Id statementId, FunctionLoweringContext context)
    {
        if (context.Terminated)
        {
            return;
        }

        switch (ResolveNode(context.Graph, statementId))
        {
            case IKun.Seq seq:
                foreach (var childId in seq.Children)
                {
                    LowerStatement(childId, context);
                }
                break;
            case IKun.Return ret:
                if (ResolveNode(context.Graph, ret.Value) is not IKun.None)
                {
                    LowerExpression(ret.Value, context, context.ReturnType);
                }
                else if (context.ReturnType != CgValueType.Void)
                {
                    EmitDefaultValue(context.Instructions, context.ReturnType);
                }

                context.Instructions.Add(new CgInstruction(CgOpcode.Return));
                context.Terminated = true;
                break;
            default:
                var valueType = LowerExpression(statementId, context, null);
                if (valueType != CgValueType.Void)
                {
                    context.Instructions.Add(new CgInstruction(CgOpcode.Pop));
                }
                break;
        }
    }

    private static CgValueType LowerExpression(Id id, FunctionLoweringContext context, CgValueType? expectedType)
    {
        return ResolveNode(context.Graph, id) switch
        {
            IKun.Constant c => EmitConst(context.Instructions, new CgOperand.I64(c.Value), CgValueType.I64),
            IKun.FloatConstant f => EmitConst(context.Instructions, new CgOperand.F64(BitConverter.UInt64BitsToDouble(f.Value)), CgValueType.F64),
            IKun.StringConstant s => EmitConst(context.Instructions, new CgOperand.Str(s.Value), CgValueType.String),
            IKun.BooleanConstant b => EmitConst(context.Instructions, new CgOperand.I32(b.Value ? 1 : 0), CgValueType.Bool),
            IKun.None => CgValueType.Void,
            IKun.Symbol symbol => EmitSymbol(symbol.Name, context),
            IKun.UnaryOp unary => EmitUnary(unary, context, expectedType),
            IKun.BinaryOp binary => EmitBinary(binary, context, expectedType),
            IKun.Apply apply => EmitApply(apply, context, expectedType),
            IKun.Return ret => LowerExpression(ret.Value, context, expectedType),
            IKun.Seq seq => seq.Children.Length > 0 ? LowerExpression(seq.Children[^1], context, expectedType) : CgValueType.Void,
            _ => CgValueType.Void
        };
    }

    private static CgValueType EmitSymbol(string name, FunctionLoweringContext context)
    {
        if (!context.Parameters.TryGetValue(name, out var index))
        {
            return CgValueType.Void;
        }

        context.Instructions.Add(new CgInstruction(CgOpcode.LoadArg, new CgOperand.Param(index, CgValueType.I32)));
        return CgValueType.I32;
    }

    private static CgValueType EmitUnary(IKun.UnaryOp unary, FunctionLoweringContext context, CgValueType? expectedType)
    {
        var operandType = LowerExpression(unary.Operand, context, expectedType);
        if (unary.Operator == "-" && operandType == CgValueType.F64)
        {
            context.Instructions.Add(new CgInstruction(CgOpcode.F64Neg));
            return CgValueType.F64;
        }

        if (unary.Operator == "-" && operandType != CgValueType.Void)
        {
            context.Instructions.Add(new CgInstruction(CgOpcode.I64Neg));
            return CgValueType.I64;
        }

        return operandType;
    }

    private static CgValueType EmitBinary(IKun.BinaryOp binary, FunctionLoweringContext context, CgValueType? expectedType)
    {
        var leftType = LowerExpression(binary.Left, context, expectedType);
        var rightType = LowerExpression(binary.Right, context, expectedType);
        var useFloat = leftType == CgValueType.F64 || rightType == CgValueType.F64 || expectedType == CgValueType.F64;

        context.Instructions.Add(new CgInstruction((binary.Operator, useFloat) switch
        {
            ("+", true) => CgOpcode.F64Add,
            ("-", true) => CgOpcode.F64Sub,
            ("*", true) => CgOpcode.F64Mul,
            ("/", true) => CgOpcode.F64Div,
            ("+", false) => CgOpcode.I64Add,
            ("-", false) => CgOpcode.I64Sub,
            ("*", false) => CgOpcode.I64Mul,
            ("/", false) => CgOpcode.I64DivS,
            _ => CgOpcode.Nop
        }));
        return useFloat ? CgValueType.F64 : CgValueType.I64;
    }

    private static CgValueType EmitApply(IKun.Apply apply, FunctionLoweringContext context, CgValueType? expectedType)
    {
        foreach (var argumentId in apply.Arguments)
        {
            LowerExpression(argumentId, context, null);
        }

        var calleeNode = ResolveNode(context.Graph, apply.Function);
        var functionName = calleeNode is IKun.Symbol calleeSymbol ? calleeSymbol.Name : "unknown";
        var signature = new CgFunctionType
        {
            Parameters = Enumerable.Repeat(CgValueType.I32, apply.Arguments.Length).ToArray(),
            Results = expectedType is { } t && t != CgValueType.Void ? [t] : []
        };

        context.Instructions.Add(new CgInstruction(CgOpcode.CallStatic, new CgOperand.FuncRef(functionName, signature)));
        return signature.Results.Count > 0 ? signature.Results[0] : CgValueType.Void;
    }

    #endregion

    #region 推断与辅助

    private static IKun? ResolveNode(EGraph<IKun> graph, Id id)
    {
        return graph.GetClass(id)?.Nodes.FirstOrDefault();
    }

    private static CgValueType InferReturnType(EGraph<IKun> graph, Id id)
    {
        return ResolveNode(graph, id) switch
        {
            IKun.Return ret when ResolveNode(graph, ret.Value) is IKun.None => CgValueType.Void,
            IKun.Return ret => InferReturnType(graph, ret.Value),
            IKun.Seq seq when seq.Children.Length > 0 => InferReturnType(graph, seq.Children[^1]),
            IKun.FloatConstant => CgValueType.F64,
            IKun.StringConstant => CgValueType.String,
            IKun.BooleanConstant => CgValueType.Bool,
            IKun.None => CgValueType.Void,
            _ => CgValueType.I64
        };
    }

    private static void EnsureReturn(FunctionLoweringContext context)
    {
        if (context.Terminated)
        {
            return;
        }

        if (context.ReturnType != CgValueType.Void)
        {
            EmitDefaultValue(context.Instructions, context.ReturnType);
        }

        context.Instructions.Add(new CgInstruction(CgOpcode.Return));
        context.Terminated = true;
    }

    private static CgValueType EmitConst(List<CgInstruction> instructions, CgOperand operand, CgValueType valueType)
    {
        instructions.Add(new CgInstruction(CgOpcode.Const, operand));
        return valueType;
    }

    private static void EmitDefaultValue(List<CgInstruction> instructions, CgValueType returnType)
    {
        instructions.Add(new CgInstruction(CgOpcode.Const, returnType switch
        {
            CgValueType.F64 => new CgOperand.F64(0d),
            CgValueType.String => new CgOperand.Str(string.Empty),
            CgValueType.Bool => new CgOperand.I32(0),
            CgValueType.Void => new CgOperand.Null(CgValueType.Void),
            _ => new CgOperand.I64(0)
        }));
    }

    private static string ToCgTypeName(CgValueType valueType)
    {
        return valueType switch
        {
            CgValueType.Void => "void",
            CgValueType.Bool => "bool",
            CgValueType.I64 => "i64",
            CgValueType.F64 => "f64",
            CgValueType.String => "string",
            _ => "i64"
        };
    }

    #endregion

    #region 上下文

    private sealed class FunctionLoweringContext
    {
        public FunctionLoweringContext(EGraph<IKun> graph, IReadOnlyList<string> parameterNames, CgValueType returnType)
        {
            Graph = graph;
            ReturnType = returnType;
            Parameters = parameterNames
                .Select((name, index) => (name, index))
                .ToDictionary(tuple => tuple.name, tuple => tuple.index, StringComparer.Ordinal);
        }

        public EGraph<IKun> Graph { get; }
        public CgValueType ReturnType { get; }
        public List<CgInstruction> Instructions { get; } = [];
        public Dictionary<string, int> Parameters { get; }
        public bool Terminated { get; set; }
    }

    #endregion
}
