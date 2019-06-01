using Valkyrie.TypeChecker.TypeSystem;

namespace Valkyrie.TypeChecker.Scope;

public sealed class Symbol
{
    public string Name { get; }
    public ValkyrieType Type { get; }
    public SymbolKind Kind { get; }
    public bool IsMutable { get; }
    public bool IsExported { get; }

    public Symbol(string name, ValkyrieType type, SymbolKind kind,
        bool isMutable = false, bool isExported = false)
    {
        Name = name;
        Type = type;
        Kind = kind;
        IsMutable = isMutable;
        IsExported = isExported;
    }
}