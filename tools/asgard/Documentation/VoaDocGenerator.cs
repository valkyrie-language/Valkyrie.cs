using System.Text;
using System.Text.RegularExpressions;

namespace Asgard.CLI.Documentation;

/// <summary>
///     GGScript 文档自动生成器
///     从 .v 源文件中提取 pub 函数、结构体、枚举的声明并生成 Markdown 文档
/// </summary>
public sealed class VoaDocGenerator
{
    private static readonly Regex FuncPattern = new(
        @"^pub\s+fn\s+(\w+)\s*\(([^)]*)\)\s*(?::\s*(\S+))?\s*{",
        RegexOptions.Multiline);

    private static readonly Regex StructPattern = new(
        @"^pub\s+struct\s+(\w+)\s*{",
        RegexOptions.Multiline);

    private static readonly Regex EnumPattern = new(
        @"^pub\s+enum\s+(\w+)\s*{",
        RegexOptions.Multiline);

    private static readonly Regex ExternFuncPattern = new(
        @"(?:\[[^\]]+\]\s*)*extern\s+(\w+)\s*\(([^)]*)\)\s*:\s*(\S+)",
        RegexOptions.Multiline);

    /// <summary>
    ///     从源文件生成 API 文档
    /// </summary>
    public DocGenerationResult GenerateDoc(string sourceDir, string outputPath)
    {
        var result = new DocGenerationResult();
        var sb = new StringBuilder();
        var vFiles = Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories);

        sb.AppendLine("# API 参考文档");
        sb.AppendLine();
        sb.AppendLine($"> 自动生成于 {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"> 扫描源文件数：{vFiles.Length}");
        sb.AppendLine();

        foreach (var file in vFiles)
        {
            var content = File.ReadAllText(file, Encoding.UTF8);
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var moduleName = Path.GetFileNameWithoutExtension(file);

            sb.AppendLine($"## {moduleName}");
            sb.AppendLine();
            sb.AppendLine($"**源文件**：`{relativePath}`");
            sb.AppendLine();

            var functions = ExtractFunctions(content);
            var structs = ExtractStructs(content);
            var enums = ExtractEnums(content);
            var externs = ExtractExterns(content);

            if (functions.Count > 0)
            {
                sb.AppendLine("### 函数");
                sb.AppendLine();
                sb.AppendLine("| 函数名 | 参数 | 返回类型 |");
                sb.AppendLine("|:---|:---|:---|");

                foreach (var func in functions)
                {
                    sb.AppendLine($"| `{func.Name}` | {func.Parameters} | {func.ReturnType} |");
                }

                sb.AppendLine();
                result.FunctionsFound += functions.Count;
            }

            if (structs.Count > 0)
            {
                sb.AppendLine("### 结构体");
                sb.AppendLine();

                foreach (var s in structs)
                {
                    sb.AppendLine($"- `{s}`");
                }

                sb.AppendLine();
                result.StructsFound += structs.Count;
            }

            if (enums.Count > 0)
            {
                sb.AppendLine("### 枚举");
                sb.AppendLine();

                foreach (var e in enums)
                {
                    sb.AppendLine($"- `{e}`");
                }

                sb.AppendLine();
                result.EnumsFound += enums.Count;
            }

            if (externs.Count > 0)
            {
                sb.AppendLine("### 外部函数声明");
                sb.AppendLine();
                sb.AppendLine("| 函数名 | 参数 | 返回类型 |");
                sb.AppendLine("|:---|:---|:---|");

                foreach (var ext in externs)
                {
                    sb.AppendLine($"| `{ext.Name}` | {ext.Parameters} | {ext.ReturnType} |");
                }

                sb.AppendLine();
                result.ExternsFound += externs.Count;
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

        result.OutputPath = outputPath;
        result.FilesProcessed = vFiles.Length;
        result.Success = true;

        return result;
    }

    private static List<(string Name, string Parameters, string ReturnType)> ExtractFunctions(string content)
    {
        var result = new List<(string, string, string)>();
        var matches = FuncPattern.Matches(content);

        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;
            var parameters = match.Groups[2].Value.Trim();
            var returnType = match.Groups[3].Success ? match.Groups[3].Value : "void";
            result.Add((name, parameters, returnType));
        }

        return result;
    }

    private static List<string> ExtractStructs(string content)
    {
        var result = new List<string>();
        var matches = StructPattern.Matches(content);

        foreach (Match match in matches)
        {
            result.Add(match.Groups[1].Value);
        }

        return result;
    }

    private static List<string> ExtractEnums(string content)
    {
        var result = new List<string>();
        var matches = EnumPattern.Matches(content);

        foreach (Match match in matches)
        {
            result.Add(match.Groups[1].Value);
        }

        return result;
    }

    private static List<(string Name, string Parameters, string ReturnType)> ExtractExterns(string content)
    {
        var result = new List<(string, string, string)>();
        var matches = ExternFuncPattern.Matches(content);

        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;
            var parameters = match.Groups[2].Value.Trim();
            var returnType = match.Groups[3].Success ? match.Groups[3].Value : "void";
            result.Add((name, parameters, returnType));
        }

        return result;
    }
}