namespace Valkyrie.Tests.StdTests;

/// <summary>
/// 标准库 API 一致性测试
/// 验证跨平台 API 命名和签名的一致性
/// </summary>
public class StdApiConsistencyTests
{
    private static readonly string ExamplesRoot = Path.GetFullPath(
        Path.Combine("..", "..", "..", "..", "..", "examples"));

    [Fact]
    public void StandardLibrary_ShouldHavePlatformSdks()
    {
        var expectedSdks = new[]
        {
            "std.adaptor.dotnet", "std.adaptor.jvm", "std.adaptor.linux", "std.adaptor.macos",
            "std.adaptor.wasip1", "std.adaptor.wasip2", "std.adaptor.wasm", "std.adaptor.windows"
        };

        foreach (var sdk in expectedSdks)
        {
            var dir = Path.Combine(ExamplesRoot, sdk);
            Assert.True(Directory.Exists(dir), $"缺少平台 SDK: {sdk}");
        }
    }

    [Fact]
    public void EachSdk_ShouldHaveLegionVon()
    {
        var sdkDirs = Directory.GetDirectories(ExamplesRoot, "std.adaptor.*");
        foreach (var sdkDir in sdkDirs)
        {
            var legionPath = Path.Combine(sdkDir, "legion.von");
            Assert.True(File.Exists(legionPath), $"{Path.GetFileName(sdkDir)} 缺少 legion.von");
        }
    }

    [Fact]
    public void EachSdk_ShouldHaveSourceDirectory()
    {
        var sdkDirs = Directory.GetDirectories(ExamplesRoot, "std.adaptor.*");
        foreach (var sdkDir in sdkDirs)
        {
            var sourceDir = Path.Combine(sdkDir, "source");
            Assert.True(Directory.Exists(sourceDir), $"{Path.GetFileName(sdkDir)} 缺少 source/ 目录");

            var vFiles = Directory.GetFiles(sourceDir, "*.v");
            Assert.NotEmpty(vFiles);
        }
    }

    [Fact]
    public void DotnetSdk_ShouldHaveConsoleBinding()
    {
        var consolePath = Path.Combine(ExamplesRoot, "std.adaptor.dotnet", "source", "console.v");
        Assert.True(File.Exists(consolePath), "dotnet SDK 缺少 console.v");

        var content = File.ReadAllText(consolePath);
        Assert.Contains("[clr(", content);
        Assert.Contains("console_write", content);
        Assert.Contains("console_write_line", content);
    }

    [Fact]
    public void JvmSdk_ShouldHaveConsoleBinding()
    {
        var consolePath = Path.Combine(ExamplesRoot, "std.adaptor.jvm", "source", "console.v");
        Assert.True(File.Exists(consolePath), "JVM SDK 缺少 console.v");

        var content = File.ReadAllText(consolePath);
        Assert.Contains("[jvm(", content);
        Assert.Contains("console_print", content);
    }

    [Fact]
    public void Wasip1Sdk_ShouldHaveIoBinding()
    {
        var ioPath = Path.Combine(ExamplesRoot, "std.adaptor.wasip1", "source", "io.v");
        Assert.True(File.Exists(ioPath), "WASI P1 SDK 缺少 io.v");

        var content = File.ReadAllText(ioPath);
        Assert.Contains("[wasi(", content);
        Assert.Contains("wasi_fd_write", content);
        Assert.Contains("wasi_fd_read", content);
    }

    [Fact]
    public void WasmSdk_ShouldHaveConsoleBinding()
    {
        var consolePath = Path.Combine(ExamplesRoot, "std.adaptor.wasm", "source", "console.v");
        Assert.True(File.Exists(consolePath), "WASM SDK 缺少 console.v");

        var content = File.ReadAllText(consolePath);
        Assert.Contains("console_log", content);
        Assert.Contains("console_error", content);
    }

    [Fact]
    public void WasmSdk_ShouldHaveDomBinding()
    {
        var domPath = Path.Combine(ExamplesRoot, "std.adaptor.wasm", "source", "dom.v");
        Assert.True(File.Exists(domPath), "WASM SDK 缺少 dom.v");

        var content = File.ReadAllText(domPath);
        Assert.Contains("dom_get_element_by_id", content);
        Assert.Contains("dom_create_element", content);
    }

    [Fact]
    public void Wasip2Sdk_ShouldHaveHttpBinding()
    {
        var httpPath = Path.Combine(ExamplesRoot, "std.adaptor.wasip2", "source", "http.v");
        Assert.True(File.Exists(httpPath), "WASI P2 SDK 缺少 http.v");

        var content = File.ReadAllText(httpPath);
        Assert.Contains("[wasi_p2(", content);
        Assert.Contains("wasi:http", content);
    }

    [Fact]
    public void AllSdkFiles_ShouldUseConsistentNamingConvention()
    {
        var sdkDirs = Directory.GetDirectories(ExamplesRoot, "std.adaptor.*");
        var violations = new List<string>();

        foreach (var sdkDir in sdkDirs)
        {
            var sourceDir = Path.Combine(sdkDir, "source");
            if (!Directory.Exists(sourceDir))
            {
                continue;
            }

            var sdkName = Path.GetFileName(sdkDir);
            foreach (var file in Directory.GetFiles(sourceDir, "*.v"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith("micro ") && !trimmed.StartsWith("func ") && !trimmed.StartsWith("extern "))
                    {
                        continue;
                    }

                    var funcName = ExtractFunctionName(trimmed);
                    if (string.IsNullOrEmpty(funcName))
                    {
                        continue;
                    }

                    if (funcName.Contains("-"))
                    {
                        violations.Add($"{sdkName}/{fileName}: 函数名 '{funcName}' 使用了连字符，应使用下划线");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    private static string ExtractFunctionName(string line)
    {
        var span = line.AsSpan();
        var parenIdx = span.IndexOf('(');
        if (parenIdx < 0)
        {
            return "";
        }

        var beforeParen = span[..parenIdx];
        var spaceIdx = beforeParen.LastIndexOf(' ');
        if (spaceIdx < 0)
        {
            return beforeParen.ToString();
        }

        return beforeParen[(spaceIdx + 1)..].ToString();
    }
}
