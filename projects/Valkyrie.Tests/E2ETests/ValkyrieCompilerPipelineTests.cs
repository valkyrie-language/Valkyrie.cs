using System.Text;
using Valkyrie.Compiler;
using Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.E2ETests;

public sealed class ValkyrieCompilerPipelineTests
{
    [Fact]
    public void CompileToTarget_MinimalHelloWorld_ShouldProduceArtifactSet()
    {
        var compiler = new ValkyrieCompiler();
        const string source = """
                              [main]
                              micro hello_world() {
                                  print("Hello World!")
                              }
                              """;
        var plan = new BuildPlan("hello_world", "nyarvm-standard");

        var artifacts = compiler.CompileToTarget(source, plan);

        Assert.NotNull(artifacts);
        Assert.Equal("hello_world.nyarvm.json", artifacts.PrimaryArtifact.Name);
        Assert.NotNull(artifacts.RunContract);
        Assert.Equal("hello_world", artifacts.RunContract!.LogicalEntry);
        Assert.NotEmpty(artifacts.PrimaryArtifact.Content);

        var json = Encoding.UTF8.GetString(artifacts.PrimaryArtifact.Content);
        Assert.Contains("\"Module\": \"hello_world\"", json, StringComparison.Ordinal);
        Assert.Contains("\"Name\": \"hello_world\"", json, StringComparison.Ordinal);
    }
}
