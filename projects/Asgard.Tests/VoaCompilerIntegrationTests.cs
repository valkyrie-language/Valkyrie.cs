using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nyar.Avatar.Web;
using Oak.Diagnostics;
using Oak.Widget;
using Valkyrie.Runtime;
using Valkyrie.Runtime.Bridge;
using Xunit;
using Xunit.Abstractions;

namespace VOA.ToolChain.Tests;

public sealed class VoaCompilerIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly VoaCompiler _compiler;
    private readonly ITestOutputHelper _output;

    public VoaCompilerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), $"voa-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _compiler = new VoaCompiler();
    }

    [Fact]
    public void Build_GGScriptOnly_ProducesWasmFiles()
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "main.v"), @"
import voa-core.console

micro main() {
    log(""hello world"")
}
");
        var outputDir = Path.Combine(_tempDir, "dist");

        var result = _compiler.Build(_tempDir, outputDir, "wasm", verbose: true);

        if (!result.Success)
        {
            _output.WriteLine($"编译失败：{result.Error}");
            foreach (var diag in _compiler.Diagnostics.Errors)
            {
                _output.WriteLine($"  诊断：{diag.Message}");
            }
        }

        Assert.True(result.Success, result.Error);
        Assert.Contains(result.OutputFiles, f => f.EndsWith(".wasm"));
        Assert.Contains(result.OutputFiles, f => f.EndsWith(".js"));
        Assert.Contains(result.OutputFiles, f => f.EndsWith(".html"));
    }

    [Fact]
    public void Build_AWSLOnly_ProducesComponentFiles()
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "Hello.awsl"), @"<widget>
    <div>Hello from AWSL</div>
</widget>");
        var outputDir = Path.Combine(_tempDir, "dist");

        var result = _compiler.Build(_tempDir, outputDir, "wasm", verbose: true);

        Assert.True(result.Success, result.Error);
        Assert.Contains(result.OutputFiles, f => f.EndsWith(".html"));
        Assert.Contains(result.OutputFiles, f => f.EndsWith(".js"));
    }

    [Fact]
    public void Build_BothGGScriptAndAWSL_ProducesFullOutput()
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(sourceDir);

        File.WriteAllText(Path.Combine(sourceDir, "main.v"), @"
import voa-core.console

micro greet(name: string) {
    log(""Hello, "" + name)
}
");
        File.WriteAllText(Path.Combine(sourceDir, "Welcome.awsl"), @"<widget>
    <div>Welcome Component</div>
</widget>");

        var outputDir = Path.Combine(_tempDir, "dist");
        var result = _compiler.Build(_tempDir, outputDir, "wasm", verbose: true);

        Assert.True(result.Success, result.Error);
        Assert.True(result.OutputFiles.Count >= 3, $"期望至少 3 个输出文件，实际 {result.OutputFiles.Count}");
    }

    [Fact]
    public void Build_PwaFlag_GeneratesServiceWorkerAndManifest()
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "main.v"), @"
import voa-core.console
micro main() { log(""pwa"") }
");
        var outputDir = Path.Combine(_tempDir, "dist");

        var result = _compiler.Build(_tempDir, outputDir, "wasm", verbose: true, pwa: true);

        Assert.True(result.Success, result.Error);
        Assert.Contains(result.OutputFiles, f => f.EndsWith("sw.js"));
        Assert.Contains(result.OutputFiles, f => f.EndsWith("manifest.json"));
        Assert.Contains(result.OutputFiles, f => f.EndsWith("offline.html"));
    }

    [Fact]
    public void Build_NoSourceFiles_ReturnsError()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "source"));
        var outputDir = Path.Combine(_tempDir, "dist");

        var result = _compiler.Build(_tempDir, outputDir, "wasm", verbose: false);

        Assert.False(result.Success);
        Assert.Contains("未找到任何源码文件", result.Error);
    }

    [Fact]
    public void Build_InvalidTarget_ReturnsError()
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "main.v"), "micro main() {}");
        var outputDir = Path.Combine(_tempDir, "dist");

        var result = _compiler.Build(_tempDir, outputDir, "jvm", verbose: false);

        Assert.False(result.Success);
        Assert.Contains("不支持的编译目标", result.Error);
    }

    [Fact]
    public void Build_SourceDirNotExists_ReturnsError()
    {
        var outputDir = Path.Combine(_tempDir, "dist");

        var result = _compiler.Build(_tempDir, outputDir, "wasm", verbose: false);

        Assert.False(result.Success);
        Assert.Contains("源码目录不存在", result.Error);
    }

    [Fact]
    public void WasmBinary_ValidationCheck()
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "main.v"), "micro main() {}");
        var outputDir = Path.Combine(_tempDir, "dist");

        var result = _compiler.Build(_tempDir, outputDir, "wasm", verbose: true);

        Assert.True(result.Success);

        var wasmFile = result.OutputFiles.FirstOrDefault(f => f.EndsWith(".wasm"));
        if (wasmFile is not null)
        {
            var wasmBytes = File.ReadAllBytes(wasmFile);
            Assert.True(wasmBytes.Length >= 4, "WASM 文件应至少 4 字节（magic + version）");
            Assert.Equal(0x00, wasmBytes[0], "WASM magic byte 1 应为 0");
            Assert.Equal(0x61, wasmBytes[1], "WASM magic byte 2 应为 'a'");
            Assert.Equal(0x73, wasmBytes[2], "WASM magic byte 3 应为 's'");
            Assert.Equal(0x6D, wasmBytes[3], "WASM magic byte 4 应为 'm'");
            _output.WriteLine($"WASM 二进制验证通过：{wasmBytes.Length} 字节");
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
        }
    }
}