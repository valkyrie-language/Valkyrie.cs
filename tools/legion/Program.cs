using Iris.CLI;
using Legion.CLI.Compiler;
using Legion.CLI.Script;
using Legion.CLI.Target;
using Legion.Package;
using Nyar;
using Nyar.Types;
using Nyar.VM;
using NyarValueType = Nyar.ValueType;
using Oak.Diagnostics;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Oak.Von;
using ValkyriePackageManagerLegion = Legion.Legion;

namespace Legion.CLI;

class Program
{
    private static readonly TargetTripleResolver TargetResolver = new();

    static int Main(string[] args)
    {
        return IrisApp.Run(args, registry =>
        {
            RegisterBuildCommand(registry);
            RegisterCleanCommand(registry);
            // 其他命令暂时禁用，直到重构完成
        });
    }

    private static void RegisterBuildCommand(CommandRegistryBuilder registry)
    {
        registry.Add("build", (string? project = null, string target = "nyar", string? output = null, bool verbose = false, bool incremental = false) =>
        {
            var projectDir = project is not null ? ResolveProjectDir(project) : Directory.GetCurrentDirectory();
            if (projectDir is null)
            {
                Console.WriteLine($"错误：找不到项目 '{project}'");
                return 1;
            }

            // 自动计算输出目录：默认为 project/dist/<triple>
            var triple = TargetResolver.Resolve(target);
            var tripleName = triple?.ToString() ?? target;
            var fullOutputDir = output ?? Path.Combine(projectDir, "dist", tripleName);
            var buildTargetOptions = ResolveBuildTargetOptions(projectDir, target, tripleName);

            var compiler = new LegionCompiler();

            Console.WriteLine($"正在构建 {projectDir} → {target}...");

            var result = compiler.Build(projectDir, fullOutputDir, target, verbose, buildTargetOptions);

            if (!result.Success)
            {
                Console.WriteLine($"构建失败：{result.Error}");
                return 1;
            }

            Console.WriteLine($"构建完成 → {result.OutputDirectory}");
            if (verbose)
            {
                foreach (var file in result.OutputFiles)
                {
                    Console.WriteLine($"  产出：{file}");
                }
            }

            return 0;
        });
    }

    private static void RegisterCleanCommand(CommandRegistryBuilder registry)
    {
        registry.Add("clean", (string? project = null, bool verbose = false) =>
        {
            var projectDir = project is not null ? ResolveProjectDir(project) : Directory.GetCurrentDirectory();
            if (projectDir is null)
            {
                Console.WriteLine($"错误：找不到项目 '{project}'");
                return 1;
            }

            var compiler = new LegionCompiler();
            compiler.Clean(projectDir);
            Console.WriteLine("已清理构建产物");
            return 0;
        });
    }

    private static string? ResolveProjectDir(string project)
    {
        if (Directory.Exists(project)) return Path.GetFullPath(project);
        var candidate = Path.Combine(Directory.GetCurrentDirectory(), project);
        if (Directory.Exists(candidate)) return Path.GetFullPath(candidate);
        return null;
    }

    /// <summary>
    ///     从 `legion.von` 中解析当前目标的构建附加选项。
    /// </summary>
    private static BuildTarget? ResolveBuildTargetOptions(string projectDir, string target, string tripleName)
    {
        var manifestPath = Path.Combine(projectDir, "legion.von");
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            var manifest = LegionManifest.Load(projectDir);
            return manifest.BuildTargets.FirstOrDefault(buildTarget =>
                string.Equals(buildTarget.Target, target, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(buildTarget.Target, tripleName, StringComparison.OrdinalIgnoreCase));
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }
}
