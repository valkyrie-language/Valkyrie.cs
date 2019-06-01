namespace Valkyrie.Interpreter.Meta;

public interface IMacroProvider {
    string? Get(string name);

    IReadOnlyDictionary<string, string> GetAll();
}
