namespace Valkyrie.TypeChecker.TypeSystem;

public sealed class ParameterType
{
    public string Name { get; }
    public ValkyrieType Type { get; }
    public bool IsMutable { get; }

    public ParameterType(string name, ValkyrieType type, bool isMutable = false)
    {
        Name = name;
        Type = type;
        IsMutable = isMutable;
    }
}