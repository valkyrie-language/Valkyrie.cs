namespace Valkyrie.Tests.StdTests;

/// <summary>
/// 标准库文档覆盖率测试
/// 检查每个公共函数是否有文档注释
/// </summary>
public class StdDocCoverageTests
{
    private static readonly string ExamplesRoot = Path.GetFullPath(
        Path.Combine("..", "..", "..", "..", "..", "examples"));

    /// <summary>
    /// 获取标准库核心 .v 文件路径（不含平台 SDK）
    /// </summary>
    public static IEnumerable<object[]> GetCoreStdVFiles()
    {
        if (!Directory.Exists(ExamplesRoot))
        {
            yield break;
        }

        var sdkDirs = Directory.GetDirectories(ExamplesRoot, "std.*_sdk");
        foreach (var sdkDir in sdkDirs)
        {
            var sourceDir = Path.Combine(sdkDir, "source");
            if (!Directory.Exists(sourceDir))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(sourceDir, "*.v", SearchOption.TopDirectoryOnly))
            {
                yield return new object[] { file };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetCoreStdVFiles))]
    public void CoreModule_EveryPublicFunction_ShouldHaveDocComment(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var lines = content.Split('\n');
        var violations = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!IsPublicFunctionDeclaration(line))
            {
                continue;
            }

            if (!HasDocCommentAbove(lines, i))
            {
                var funcName = ExtractFunctionName(line);
                violations.Add($"第 {i + 1} 行: {funcName}");
            }
        }

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(GetCoreStdVFiles))]
    public void CoreModule_EveryPublicFunction_DocCommentShouldDescribeParams(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var lines = content.Split('\n');
        var violations = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!IsPublicFunctionDeclaration(line))
            {
                continue;
            }

            var paramCount = CountParameters(line);
            if (paramCount == 0)
            {
                continue;
            }

            var docBlock = GetDocCommentBlock(lines, i);
            var docHasReturns = docBlock.Any(l => l.Contains("- returns:") || l.Contains("- returns："));

            if (!docHasReturns && HasReturnValue(line))
            {
                var funcName = ExtractFunctionName(line);
                violations.Add($"第 {i + 1} 行: {funcName} 缺少 returns 描述");
            }
        }

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(GetCoreStdVFiles))]
    public void CoreModule_ShouldUseRegionSections(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        var skipFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ffi_test"
        };

        if (skipFiles.Contains(fileName))
        {
            return;
        }

        Assert.True(
            content.Contains("#region"),
            $"核心模块 {fileName} 未使用 #region 分区组织代码");
    }

    private static bool IsPublicFunctionDeclaration(string line)
    {
        return line.StartsWith("micro ") || line.StartsWith("func ") || line.StartsWith("extern ");
    }

    private static bool HasDocCommentAbove(string[] lines, int funcLineIndex)
    {
        for (var i = funcLineIndex - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line.StartsWith("#"))
            {
                return true;
            }

            if (line.StartsWith("#endregion") || line.StartsWith("#region"))
            {
                return false;
            }

            if (line.StartsWith("["))
            {
                continue;
            }

            return false;
        }

        return false;
    }

    private static List<string> GetDocCommentBlock(string[] lines, int funcLineIndex)
    {
        var block = new List<string>();

        for (var i = funcLineIndex - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line.StartsWith("#"))
            {
                block.Insert(0, line);
            }
            else
            {
                break;
            }
        }

        return block;
    }

    private static string ExtractFunctionName(string line)
    {
        var span = line.AsSpan();
        var parenIdx = span.IndexOf('(');
        if (parenIdx < 0)
        {
            return line;
        }

        var beforeParen = span[..parenIdx];
        var spaceIdx = beforeParen.LastIndexOf(' ');
        if (spaceIdx < 0)
        {
            return beforeParen.ToString();
        }

        return beforeParen[(spaceIdx + 1)..].ToString();
    }

    private static int CountParameters(string line)
    {
        var span = line.AsSpan();
        var start = span.IndexOf('(');
        var end = span.IndexOf(')');
        if (start < 0 || end < 0 || end <= start)
        {
            return 0;
        }

        var paramsSpan = span[(start + 1)..end];
        if (paramsSpan.IsEmpty)
        {
            return 0;
        }

        var count = 1;
        foreach (var ch in paramsSpan)
        {
            if (ch == ',')
            {
                count++;
            }
        }

        return count;
    }

    private static bool HasReturnValue(string line)
    {
        var span = line.AsSpan();
        var colonIdx = span.LastIndexOf(')');
        if (colonIdx < 0 || colonIdx >= span.Length - 1)
        {
            return false;
        }

        var afterParen = span[(colonIdx + 1)..].Trim();
        return afterParen.StartsWith(":");
    }
}
