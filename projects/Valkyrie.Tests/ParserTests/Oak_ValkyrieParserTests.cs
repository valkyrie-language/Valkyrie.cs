using Oak.Valkyrie;
using Oak.Valkyrie.AST.Declaration;
using Oak.Valkyrie.AST.ECS;
using Oak.Valkyrie.AST.Statement;
using Oak.Valkyrie.AST.Template;
using Oak.Valkyrie.AST.Term;
using Oak.Valkyrie.AST.Type;

namespace Valkyrie.Tests.ParserTests;

public class Oak_ValkyrieParserTests
{
    private readonly ValkyrieLanguage _language = ValkyrieLanguage.Standard;
    private readonly ValkyrieLanguage _languageWithSchema = ValkyrieLanguage.Schema;
    private readonly ValkyrieLanguage _languageWithShader = ValkyrieLanguage.Shader;

    private ProgramRoot Parse(string source, ValkyrieLanguage? language = null)
    {
        throw new NotImplementedException();
    }

    public static ProgramRoot AssertParseResultNotNull(ProgramRoot result) => result;

    public static void AssertParseResultNotNull(object? result)
    {
        if (result is null)
        {
            throw new Exception("Parse result is null");
        }
    }
}
