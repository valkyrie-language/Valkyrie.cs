using System.Text;
using System.Text.Json;
using Nyar.Semantic;
using Nyar.Types;
using Oak.Diagnostics;
using Oak.Syntax;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Valkyrie.Compiler.Hir;
using Valkyrie.Compiler.Lir;
using Valkyrie.Compiler.Mir;
using Valkyrie.Compiler.Pipeline;
using Valkyrie.Compiler.Targets;
using Valkyrie.TypeChecker;
using SemanticDiagnosticLevel = Oak.Diagnostics.DiagnosticLevel;
using ValkyrieTypeChecker = Valkyrie.TypeChecker.TypeChecker;

namespace Valkyrie.Compiler;

/// <summary>
/// Valkyrie 新编译器门面。
/// 当前先固定正确的 `HIR/MIR/LIR` API 形状，后续逐步补齐实现。
/// </summary>
public sealed class ValkyrieCompiler
{
    private readonly DiagnosticSink _diagnostics;
    private readonly ValkyrieLexer _lexer;
    private readonly ValkyrieParser _parser;
    private readonly ValkyrieTypeChecker _typeChecker;
    private readonly HirBuilder _hirBuilder;
    private readonly MirBuilder _mirBuilder;
    private readonly LirBuilder _lirBuilder;
    private readonly CanonicalTripleRegistry _canonicalTripleRegistry;

    public ValkyrieCompiler()
    {
        _diagnostics = new DiagnosticSink();
        _lexer = new ValkyrieLexer(_diagnostics);
        _parser = new ValkyrieParser(ValkyrieLanguage.Standard, _diagnostics);
        _typeChecker = new ValkyrieTypeChecker();
        _hirBuilder = new HirBuilder();
        _mirBuilder = new MirBuilder();
        _lirBuilder = new LirBuilder();
        _canonicalTripleRegistry = new CanonicalTripleRegistry();
    }

    public DiagnosticSink Diagnostics => _diagnostics;

    public IReadOnlyList<GreenLeafNode> Lex(string source)
    {
        return _lexer.Tokenize(source);
    }

    public Oak.Valkyrie.AST.CompilationUnit Parse(IReadOnlyList<GreenLeafNode> tokens)
    {
        return (Oak.Valkyrie.AST.CompilationUnit)_parser.Parse(tokens);
    }

    public SemanticModel Analyze(Oak.Valkyrie.AST.CompilationUnit ast, BuildPlan plan)
    {
        var filePath = plan.FilePath ?? plan.ModuleName;
        var typeCheckResult = _typeChecker.Check(ast, filePath);

        var semanticModel = new SemanticModel(filePath, new SymbolTable());
        foreach (var diagnostic in typeCheckResult.Diagnostics)
        {
            var level = diagnostic.Severity switch
            {
                Valkyrie.TypeChecker.DiagnosticSeverity.Error => SemanticDiagnosticLevel.Error,
                Valkyrie.TypeChecker.DiagnosticSeverity.Warning => SemanticDiagnosticLevel.Warning,
                _ => SemanticDiagnosticLevel.Info
            };
            var span = new TextSpan(0, 0);
            semanticModel.AddDiagnostic(new SemanticDiagnostic(level, diagnostic.Message, span, diagnostic.Code, filePath: filePath));
        }

        return semanticModel;
    }

    public HirModule BuildHir(Oak.Valkyrie.AST.CompilationUnit ast, SemanticModel semantics, BuildPlan plan)
    {
        return _hirBuilder.Build(ast, semantics, plan.ModuleName);
    }

    public MirModule BuildMir(HirModule hir, BuildPlan plan, TargetContract targetContract)
    {
        var targetArchTag = ResolveTargetArchTag(targetContract);
        return _mirBuilder.Build(hir, targetArchTag);
    }

    public LirModule BuildLir(MirModule mir, BuildPlan plan)
    {
        return _lirBuilder.Build(mir);
    }

    public ArtifactSet CompileToTarget(string source, BuildPlan plan)
    {
        _diagnostics.Clear();
        var targetContract = _canonicalTripleRegistry.Resolve(plan.CanonicalTriple);

        var tokens = Lex(source);
        if (_diagnostics.HasErrors)
        {
            throw new InvalidOperationException("词法分析失败，无法继续编译。");
        }

        var ast = Parse(tokens);
        if (_diagnostics.HasErrors)
        {
            throw new InvalidOperationException("语法分析失败，无法继续编译。");
        }

        var semantics = Analyze(ast, plan);
        if (semantics.HasErrors)
        {
            throw new InvalidOperationException("语义分析失败，无法继续编译。");
        }

        var hir = BuildHir(ast, semantics, plan);
        var mir = BuildMir(hir, plan, targetContract);
        var lir = BuildLir(mir, plan);

        var primaryArtifact = BuildPrimaryArtifact(lir, plan, targetContract);
        var runContract = BuildRunContract(hir);
        return new ArtifactSet(primaryArtifact, runContract: runContract);
    }

    private static CompilerArtifact BuildPrimaryArtifact(LirModule lir, BuildPlan plan, TargetContract targetContract)
    {
        var model = new
        {
            Module = lir.Module.Name,
            Target = targetContract.CanonicalTriple,
            Backend = targetContract.BackendFamily,
            Abi = targetContract.AbiProfile,
            Functions = lir.Module.Functions.Select(function => new
            {
                function.Name,
                function.ReturnType,
                Parameters = function.Parameters.Select(parameter => new { parameter.Name, parameter.Type }).ToArray(),
                Instructions = function.Instructions.Select(instruction => new
                {
                    Opcode = instruction.Opcode.ToString(),
                    Operands = instruction.Operands.Select(FormatOperand).ToArray()
                }).ToArray()
            }).ToArray(),
            Exports = lir.Module.Exports.Select(export => new
            {
                export.Name,
                Kind = export.Kind.ToString(),
                export.FunctionIndex
            }).ToArray()
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(model, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        var artifactName = $"{plan.ModuleName}.{targetContract.BackendFamily.ToLowerInvariant()}.json";
        return new CompilerArtifact(artifactName, bytes, "application/json");
    }

    private static string FormatOperand(Nyar.Assembler.CgOperand operand)
    {
        return operand switch
        {
            Nyar.Assembler.CgOperand.I32 i32 => $"i32:{i32.Value}",
            Nyar.Assembler.CgOperand.I64 i64 => $"i64:{i64.Value}",
            Nyar.Assembler.CgOperand.F32 f32 => $"f32:{f32.Value}",
            Nyar.Assembler.CgOperand.F64 f64 => $"f64:{f64.Value}",
            Nyar.Assembler.CgOperand.Str str => $"str:{str.Value}",
            Nyar.Assembler.CgOperand.Local local => $"local:{local.Index}:{local.Type}",
            Nyar.Assembler.CgOperand.Param param => $"param:{param.Index}:{param.Type}",
            Nyar.Assembler.CgOperand.Label label => $"label:{label.Name}",
            Nyar.Assembler.CgOperand.FuncRef funcRef => $"funcref:{funcRef.Name}",
            Nyar.Assembler.CgOperand.Const @const => $"const:{@const.PoolIndex}:{@const.Type}",
            Nyar.Assembler.CgOperand.Null @null => $"null:{@null.Type}",
            _ => operand.ToString() ?? string.Empty
        };
    }

    private static RunContract BuildRunContract(HirModule hir)
    {
        var logicalEntry = hir.Functions.FirstOrDefault(function => function.IsLogicalEntry)?.Name ?? "main";
        var invocationShape = $"{hir.Name}.{logicalEntry}(...)";
        var validationCommand = $"vcc run --module {hir.Name} --entry {logicalEntry}";
        return new RunContract(logicalEntry, logicalEntry, invocationShape, validationCommand);
    }

    private static string ResolveTargetArchTag(TargetContract targetContract)
    {
        return targetContract.BackendFamily switch
        {
            "JVM" => "jvm",
            "CLR" => "clr",
            "WASM" => targetContract.CanonicalTriple.StartsWith("wasm64-", StringComparison.OrdinalIgnoreCase)
                ? "wasm64"
                : "wasm32",
            "NyarVM" => "nyarvm",
            _ => string.Empty
        };
    }
}
