using VOA.ToolChain.Compiler;
using Xunit;

namespace VOA.ToolChain.Tests;

public sealed class VoaCompilerTests
{
    [Fact]
    public void Build_NonExistentProjectDir_ReturnsFailure()
    {
        var compiler = new VoaCompiler();
        var result = compiler.Build(
            Path.Combine(Path.GetTempPath(), "voa-nonexistent-" + Guid.NewGuid()),
            Path.Combine(Path.GetTempPath(), "voa-output-" + Guid.NewGuid()),
            "wasm",
            false
        );

        Assert.False(result.Success);
        Assert.Contains("源码目录不存在", result.Error);
    }

    [Fact]
    public void Build_EmptySourceDir_ReturnsFailure()
    {
        var projectDir = Path.Combine(Path.GetTempPath(), "voa-empty-" + Guid.NewGuid());
        var sourceDir = Path.Combine(projectDir, "source");
        var outputDir = Path.Combine(Path.GetTempPath(), "voa-output-" + Guid.NewGuid());

        Directory.CreateDirectory(sourceDir);

        try
        {
            var compiler = new VoaCompiler();
            var result = compiler.Build(projectDir, outputDir, "wasm", false);

            Assert.False(result.Success);
            Assert.Contains("未找到任何源码文件", result.Error);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void Build_UnsupportedTarget_ReturnsFailure()
    {
        var projectDir = Path.Combine(Path.GetTempPath(), "voa-unsupported-" + Guid.NewGuid());
        var sourceDir = Path.Combine(projectDir, "source");
        var outputDir = Path.Combine(Path.GetTempPath(), "voa-output-" + Guid.NewGuid());

        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "app.awsl"), """
                                                               <widget>
                                                                   <div>Test</div>
                                                               </widget>
                                                               """);

        try
        {
            var compiler = new VoaCompiler();
            var result = compiler.Build(projectDir, outputDir, "native", false);

            Assert.False(result.Success);
            Assert.Contains("不支持的编译目标", result.Error);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void Build_SingleAwslFile_Succeeds()
    {
        var projectDir = Path.Combine(Path.GetTempPath(), "voa-awsl-" + Guid.NewGuid());
        var sourceDir = Path.Combine(projectDir, "source");
        var outputDir = Path.Combine(Path.GetTempPath(), "voa-output-" + Guid.NewGuid());

        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "app.awsl"), """
                                                               component App {
                                                                   template {
                                                                       <div>Hello VOA</div>
                                                                   }
                                                               }
                                                               """);

        try
        {
            var compiler = new VoaCompiler();
            var result = compiler.Build(projectDir, outputDir, "wasm", false);

            Assert.True(result.Success, result.Error ?? "构建失败");
            Assert.True(Directory.Exists(outputDir));
        }
        finally
        {
            if (Directory.Exists(projectDir)) Directory.Delete(projectDir, true);
            if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public void BuildWasm_SameAsBuildWithWasmTarget()
    {
        var projectDir = Path.Combine(Path.GetTempPath(), "voa-wasm-" + Guid.NewGuid());
        var sourceDir = Path.Combine(projectDir, "source");
        var outputDir = Path.Combine(Path.GetTempPath(), "voa-output-" + Guid.NewGuid());

        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "hello.awsl"), """
                                                                 component Hello {
                                                                     template {
                                                                         <span>Hi</span>
                                                                     }
                                                                 }
                                                                 """);

        try
        {
            var compiler = new VoaCompiler();
            var result = compiler.BuildWasm(projectDir, outputDir, false);

            Assert.True(result.Success, result.Error ?? "构建失败");
        }
        finally
        {
            if (Directory.Exists(projectDir)) Directory.Delete(projectDir, true);
            if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
        }
    }
}