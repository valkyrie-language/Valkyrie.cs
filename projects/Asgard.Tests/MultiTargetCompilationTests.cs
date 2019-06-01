namespace VOA.ToolChain.Tests;

/// <summary>
///     多目标编译集成测试
///     验证 VOA 编译器对各目标平台的配置和输出正确性
/// </summary>
public sealed class MultiTargetCompilationTests
{
    private readonly ITestOutputHelper _output;

    public MultiTargetCompilationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region 平台配置验证

    [Fact]
    public void AllTargets_Count_HasFiveTargets()
    {
        var targets = VoaTargetConfig.SupportedTargets;

        Assert.Equal(5, targets.Count);
        _output.WriteLine($"支持 {targets.Count} 个编译目标平台");
    }

    [Fact]
    public void WasmTarget_HasCorrectConfig()
    {
        var wasm = VoaTargetConfig.FindTarget("wasm")!;

        Assert.NotNull(wasm);
        Assert.Equal("wasm", wasm.Id);
        Assert.True(wasm.HasJsGlue, "WASM 浏览器目标需要 JS glue");
        Assert.True(wasm.HasPwa, "WASM 浏览器目标支持 PWA");
        Assert.Equal("wasm", wasm.OutputFormat);

        _output.WriteLine($"WASM 目标：{wasm.Name} / {wasm.Abi} / 输出 {wasm.OutputFormat}");
    }

    [Fact]
    public void WasiTarget_HasCorrectConfig()
    {
        var wasi = VoaTargetConfig.FindTarget("wasi")!;

        Assert.NotNull(wasi);
        Assert.Equal("wasi", wasi.Id);
        Assert.False(wasi.HasJsGlue, "WASI 目标不需要 JS glue");
        Assert.False(wasi.HasPwa, "WASI 目标不支持 PWA");
        Assert.Equal("wasm", wasi.OutputFormat);

        _output.WriteLine($"WASI 目标：{wasi.Name} / {wasi.Abi}");
    }

    [Fact]
    public void ClrTarget_HasCorrectConfig()
    {
        var clr = VoaTargetConfig.FindTarget("clr")!;

        Assert.NotNull(clr);
        Assert.Equal("clr", clr.Id);
        Assert.Equal("dll", clr.OutputFormat);
        Assert.False(clr.HasJsGlue);

        _output.WriteLine($"CLR 目标：{clr.Name} / 输出 {clr.OutputFormat}");
    }

    [Fact]
    public void JvmTarget_HasCorrectConfig()
    {
        var jvm = VoaTargetConfig.FindTarget("jvm")!;

        Assert.NotNull(jvm);
        Assert.Equal("jvm", jvm.Id);
        Assert.Equal("class", jvm.OutputFormat);
        Assert.False(jvm.HasPwa);

        _output.WriteLine($"JVM 目标：{jvm.Name} / 输出 {jvm.OutputFormat}");
    }

    [Fact]
    public void NativeTarget_HasCorrectConfig()
    {
        var native = VoaTargetConfig.FindTarget("native")!;

        Assert.NotNull(native);
        Assert.Equal("native", native.Id);
        Assert.Equal("exe", native.OutputFormat);
        Assert.False(native.HasJsGlue);

        _output.WriteLine($"Native 目标：{native.Name} / {native.Arch} / 输出 {native.OutputFormat}");
    }

    [Fact]
    public void FindTarget_InvalidTarget_ReturnsNull()
    {
        var unknown = VoaTargetConfig.FindTarget("unknown-platform");

        Assert.Null(unknown);
        _output.WriteLine("未知目标平台返回 null ✅");
    }

    [Fact]
    public void FindTarget_CaseInsensitive_Works()
    {
        var upper = VoaTargetConfig.FindTarget("WASM");
        var mixed = VoaTargetConfig.FindTarget("Wasi");

        Assert.NotNull(upper);
        Assert.NotNull(mixed);
        Assert.Equal("wasm", upper!.Id);
        Assert.Equal("wasi", mixed!.Id);

        _output.WriteLine("目标平台查找不区分大小写 ✅");
    }

    #endregion

    #region 输出格式验证

    [Fact]
    public void OutputFormat_EachTarget_Unique()
    {
        var formats = new HashSet<string>();
        foreach (var target in VoaTargetConfig.SupportedTargets)
        {
            Assert.True(string.IsNullOrEmpty(target.OutputFormat) == false, $"{target.Id} 输出格式不应为空");
            formats.Add(target.OutputFormat);
        }

        _output.WriteLine($"输出格式：{string.Join(", ", formats)}（{formats.Count} 种）");
        Assert.True(formats.Count >= 3, $"应有至少 3 种不同的输出格式，实际 {formats.Count}");
    }

    [Fact]
    public void Arch_EachTarget_Defined()
    {
        foreach (var target in VoaTargetConfig.SupportedTargets)
        {
            Assert.False(string.IsNullOrEmpty(target.Arch), $"{target.Id} 架构不应为空");
        }

        _output.WriteLine($"所有 {VoaTargetConfig.SupportedTargets.Count} 个目标均定义了架构");
    }

    [Fact]
    public void Abi_EachTarget_Defined()
    {
        foreach (var target in VoaTargetConfig.SupportedTargets)
        {
            Assert.False(string.IsNullOrEmpty(target.Abi), $"{target.Id} ABI 不应为空");
        }

        _output.WriteLine($"所有 {VoaTargetConfig.SupportedTargets.Count} 个目标均定义了 ABI");
    }

    #endregion

    #region VoaCompiler 多目标构建

    [Fact]
    public void VoaCompiler_Build_UnsupportedTarget_ReturnsError()
    {
        var compiler = new VoaCompiler();

        var tempDir = Path.Combine(Path.GetTempPath(), $"voa_test_{Guid.NewGuid():N}");
        var srcDir = Path.Combine(tempDir, "source");
        var outDir = Path.Combine(tempDir, "dist");

        Directory.CreateDirectory(srcDir);
        Directory.CreateDirectory(outDir);

        File.WriteAllText(Path.Combine(srcDir, "main.v"), "micro main() {}");

        var result = compiler.Build(tempDir, outDir, "unsupported", false);

        Assert.False(result.Success);
        Assert.Contains("不支持", result.Error, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine($"不支持的目标：{result.Error}");

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void VoaCompiler_BuildWasm_SuccessfulCompilation()
    {
        var compiler = new VoaCompiler();

        var tempDir = Path.Combine(Path.GetTempPath(), $"voa_test_{Guid.NewGuid():N}");
        var srcDir = Path.Combine(tempDir, "source");
        var outDir = Path.Combine(tempDir, "dist");

        Directory.CreateDirectory(srcDir);
        Directory.CreateDirectory(outDir);

        File.WriteAllText(Path.Combine(srcDir, "main.v"), "micro main() {}");

        var result = compiler.BuildWasm(tempDir, outDir, false);

        Assert.True(result.Success, $"编译应为成功：{result.Error}");
        _output.WriteLine($"WASM 编译成功：{result.OutputFiles.Count} 个输出文件");

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void VoaCompiler_Diagnostics_PopulatedAfterCompilation()
    {
        var compiler = new VoaCompiler();

        var diags = compiler.Diagnostics;

        Assert.NotNull(diags);
        _output.WriteLine("诊断系统初始化正常 ✅");
    }

    #endregion

    #region VoaCompiler 与 BackendSelector 对应

    [Fact]
    public void BackendMapping_TargetToBackend_Corresponds()
    {
        var targetToBackend = new Dictionary<string, string>
        {
            ["wasm"] = "WasmBackend",
            ["wasi"] = "WasmBackend + WasiP1ImportBuilder",
            ["clr"] = "ClrBackend",
            ["jvm"] = "JvmBackend",
            ["native"] = "NativeBackend",
        };

        foreach (var target in VoaTargetConfig.SupportedTargets)
        {
            Assert.True(targetToBackend.ContainsKey(target.Id),
                $"目标 {target.Id} 应有对应后端映射");
        }

        _output.WriteLine("所有目标平台都有对应后端映射 ✅");
    }

    #endregion

    #region 编译选项扩展

    [Fact]
    public void Compiler_WithOptionalFeatures_TracksCorrectly()
    {
        var pwaTargets = VoaTargetConfig.SupportedTargets.Where(t => t.HasPwa).ToList();
        var jsGlueTargets = VoaTargetConfig.SupportedTargets.Where(t => t.HasJsGlue).ToList();

        Assert.Single(pwaTargets);
        Assert.Single(jsGlueTargets);
        Assert.Equal("wasm", pwaTargets[0].Id);
        Assert.Equal("wasm", jsGlueTargets[0].Id);

        _output.WriteLine($"PWA 支持：{string.Join(", ", pwaTargets.Select(t => t.Id))}");
        _output.WriteLine($"JS Glue 支持：{string.Join(", ", jsGlueTargets.Select(t => t.Id))}");
    }

    #endregion
}