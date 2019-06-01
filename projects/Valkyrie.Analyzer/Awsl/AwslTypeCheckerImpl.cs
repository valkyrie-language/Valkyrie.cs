using Nyar.Semantic;
using Oak.Widget;

namespace Valkyrie.Analyzer.Awsl;

/// <summary>
///     AWSL 类型检查器实现
/// </summary>
public sealed class AwslTypeCheckerImpl : Nyar.Semantic.TypeChecker {
    public AwslTypeCheckerImpl() {
        Bridge = new AwslSemanticBridge();
    }

    public AwslSemanticBridge Bridge { get; }

    public Nyar.Semantic.SemanticModel CheckWidget(WidgetParseResult parseResult, string? filePath = null) {
        return Bridge.BuildSemanticModel(parseResult, filePath ?? "");
    }

    public Nyar.Semantic.IType InferPropertyType(WidgetProperty prop) {
        var declaredType = Bridge.ConvertWidgetPropertyType(prop.TypeName);

        if (declaredType is Nyar.Semantic.AutoType or Nyar.Semantic.UnknownType &&
            prop.DefaultValueKind != WidgetValueKind.None)
            return Bridge.InferValueKindType(prop.DefaultValueKind);

        return declaredType;
    }

    public override IType InferType(ISymbol symbol) {
        throw new NotImplementedException();
    }

    public override IType InferTypeOfExpression(object node) {
        throw new NotImplementedException();
    }

    public override bool CheckType(IType expected, IType actual, out SemanticDiagnostic? diagnostic) {
        throw new NotImplementedException();
    }

    public override IReadOnlyList<SemanticDiagnostic> CheckAllTypes(SemanticModel model) {
        throw new NotImplementedException();
    }
}
