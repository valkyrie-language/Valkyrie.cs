namespace Valkyrie.TypeChecker.Scope;

public sealed class Scope
{
    private readonly Dictionary<string, Symbol> _symbols = new(StringComparer.Ordinal);
    private readonly List<Scope> _children = [];
    private readonly Scope? _parent;

    public Scope? Parent => _parent;
    public IReadOnlyList<Scope> Children => _children;
    public IReadOnlyDictionary<string, Symbol> Symbols => _symbols;

    public Scope(Scope? parent = null)
    {
        _parent = parent;
        _parent?._children.Add(this);
    }

    public bool Define(Symbol symbol)
    {
        if (_symbols.ContainsKey(symbol.Name))
        {
            return false;
        }

        _symbols[symbol.Name] = symbol;
        return true;
    }

    public Symbol? Resolve(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        return _parent?.Resolve(name);
    }

    public Symbol? ResolveLocal(string name)
    {
        return _symbols.TryGetValue(name, out var symbol) ? symbol : null;
    }

    public IEnumerable<Symbol> GetAllAccessibleSymbols()
    {
        var result = new List<Symbol>(_symbols.Values);

        if (_parent is not null)
        {
            result.AddRange(_parent.GetAllAccessibleSymbols());
        }

        return result;
    }
}