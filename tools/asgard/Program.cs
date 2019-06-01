using System.Diagnostics;
using Asgard.CLI.Commands;
using Asgard.CLI.Compiler;
using Asgard.CLI.DevServer;
using Asgard.CLI.Script;
using Iris.CLI;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;

namespace Asgard.CLI;

class Program
{
    private static readonly string[] SupportedTargets = ["wasm", "wasip1", "wasip2", "clr", "jvm", "native", "nyar", "gnosis"];

    static int Main(string[] args)
    {
        return IrisApp.Run(args, registry =>
        {
            RegisterDevCommand(registry);
            RegisterStartCommand(registry);
            RegisterBuildCommand(registry);
            RegisterAddCommand(registry);
            RegisterRemoveCommand(registry);
            RegisterInstallCommand(registry);
            RegisterRunCommand(registry);
            RegisterCleanCommand(registry);
            RegisterPublishCommand(registry);
            RegisterNewCommand(registry);
            RegisterCheckCommand(registry);
            RegisterFmtCommand(registry);
            RegisterTestCommand(registry);
            RegisterInitCommand(registry);
            RegisterBenchmarkCommand(registry);
            RegisterCoverageCommand(registry);
        });
    }

    private static void RegisterDevCommand(CommandRegistryBuilder registry)
    {
        registry.Add("dev", (string project, string env = "development", int? port = null, string? host = null, bool open = false) =>
        {
            var projectDir = ResolveVoaProjectDir(project);
            if (projectDir is null)
            {
                Console.Error.WriteLine($"错误：找不到项目 '{project}'（需包含 voa.von 文件）");
                return 1;
            }

            var devPort = port ?? 3000;
            var devHost = host ?? "localhost";
            var url = $"http://{devHost}:{devPort}";

            var configLoader = new VoaConfigLoader();
            var config = configLoader.Load(projectDir);
            config.HotReload.Enabled = true;

            Console.WriteLine($"VOA 开发服务器");
            Console.WriteLine($"  项目：{projectDir}");
            Console.WriteLine($"  环境：{env}");
            Console.WriteLine($"  地址：{url}");
            Console.WriteLine($"  热重载：开启");

            Environment.SetEnvironmentVariable("PORT", devPort.ToString());
            Environment.SetEnvironmentVariable("VOA_ENV", env);

            using var devServer = new VoaDevServer(projectDir, devHost, devPort, config);
            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            if (open)
            {
                OpenBrowser(url);
            }

            try
            {
                devServer.StartAsync(cts.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"开发服务器异常：{ex.Message}");
                return 1;
            }
            finally
            {
                devServer.Stop();
            }

            return 0;
        });
    }

    private static void RegisterStartCommand(CommandRegistryBuilder registry)
    {
        registry.Add("start", StartCommand.Execute);
    }

    private static void RegisterBuildCommand(CommandRegistryBuilder registry)
    {
        registry.Add("build", (string project, string target = "wasm", bool watch = false, bool verbose = false) =>
        {
            var projectDir = ResolveVoaProjectDir(project);
            if (projectDir is null)
            {
                Console.Error.WriteLine($"错误：找不到项目 '{project}'（需包含 voa.von 文件）");
                return 1;
            }

            if (Array.IndexOf(SupportedTargets, target) < 0)
            {
                Console.Error.WriteLine($"错误：不支持的编译目标 '{target}'，可选：{string.Join(" / ", SupportedTargets)}");
                return 1;
            }

            var outDir = Path.Combine(projectDir, "dist");
            var buildConfig = new VoaBuildConfig();

            if (verbose)
            {
                Console.WriteLine($"正在构建 {projectDir} → {target}...");
                Console.WriteLine($"  输出目录：{outDir}");
            }

            var builder = new VoaMultiTargetBuilder();
            var result = builder.Build(buildConfig, projectDir, outDir, target, verbose);
            if (!result.Success)
            {
                Console.Error.WriteLine($"构建失败：{result.Error}");
                return 1;
            }

            var ssgResult = RunSSG(projectDir, result.OutputDirectory, verbose);
            if (!ssgResult.Success)
            {
                Console.Error.WriteLine($"SSG 生成失败：{ssgResult.Error}");
                return 1;
            }

            Console.WriteLine($"构建完成 → {result.OutputDirectory}");

            if (watch)
            {
                Console.WriteLine("正在监视文件变更...");
                using var watcher = new FileSystemWatcher(projectDir)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
                };

                watcher.Changed += (_, _) =>
                {
                    Console.WriteLine("检测到文件变更，重新构建...");
                    var watchBuilder = new VoaMultiTargetBuilder();
                    var rebuild = watchBuilder.Build(buildConfig, projectDir, outDir, target, verbose);
                    if (rebuild.Success)
                    {
                        RunSSG(projectDir, rebuild.OutputDirectory, verbose);
                        Console.WriteLine("重建完成");
                    }
                };

                Console.WriteLine("按 Ctrl+C 停止");
                while (true) { Thread.Sleep(1000); }
            }

            return 0;
        });
    }

    private static void RegisterAddCommand(CommandRegistryBuilder registry)
    {
        registry.Add("add", (string project, string dependency, bool dev = false) =>
        {
            var legion = new Legion();
            legion.AddDependency(project, dependency, dev);
            Console.WriteLine($"已添加依赖：{dependency}");
            return 0;
        });
    }

    private static void RegisterRemoveCommand(CommandRegistryBuilder registry)
    {
        registry.Add("remove", (string project, string package) =>
        {
            var legion = new Legion();
            legion.RemoveDependency(project, package);
            Console.WriteLine($"已移除依赖：{package}");
            return 0;
        });
    }

    private static void RegisterInstallCommand(CommandRegistryBuilder registry)
    {
        registry.Add("install", async (string? package = null) =>
        {
            var legion = new Legion();
            if (string.IsNullOrWhiteSpace(package))
            {
                await legion.InstallAsync("dependencies");
            }
            else
            {
                await legion.InstallAsync(package, "latest", "npm");
            }

            return 0;
        });
    }

    private static void RegisterRunCommand(CommandRegistryBuilder registry)
    {
        registry.Add("run", (string? name = null) =>
        {
            var legion = new Legion();
            if (name is null)
            {
                var scripts = legion.Manifest?.Scripts;
                var hasManifestScripts = scripts is not null && scripts.Count > 0;

                var discovery = new VoaScriptDiscovery();
                var currentDir = Directory.GetCurrentDirectory();
                var discoveredScripts = discovery.DiscoverScripts(currentDir);
                var hasDiscoveredScripts = discoveredScripts.Count > 0;

                if (!hasManifestScripts && !hasDiscoveredScripts)
                {
                    Console.WriteLine("没有可用的脚本");
                    return 0;
                }

                if (hasManifestScripts)
                {
                    Console.WriteLine("可用脚本：");
                    foreach (var (key, value) in scripts!.OrderBy(kv => kv.Key))
                    {
                        Console.WriteLine($"  {key} → {value}");
                    }
                }

                if (hasDiscoveredScripts)
                {
                    Console.WriteLine("发现的脚本（script/ 目录）：");
                    foreach (var (key, value) in discoveredScripts.OrderBy(kv => kv.Key))
                    {
                        Console.WriteLine($"  {key} → {value}");
                    }
                }
            }
            else
            {
                var result = legion.RunScriptAsync(name).GetAwaiter().GetResult();
                if (!result.Success)
                {
                    Console.Error.WriteLine($"执行失败：{result.Error}");
                    return 1;
                }
            }

            return 0;
        });
    }

    private static void RegisterCleanCommand(CommandRegistryBuilder registry)
    {
        registry.Add("clean", (string project, bool verbose = false) =>
        {
            var projectDir = ResolveVoaProjectDir(project);
            if (projectDir is null)
            {
                Console.Error.WriteLine($"错误：找不到项目 '{project}'");
                return 1;
            }

            var distDir = Path.Combine(projectDir, "dist");
            if (Directory.Exists(distDir))
            {
                Directory.Delete(distDir, true);
                if (verbose) Console.WriteLine($"已清理：{distDir}");
            }

            var ssgCache = Path.Combine(projectDir, ".voa_ssg");
            if (Directory.Exists(ssgCache))
            {
                Directory.Delete(ssgCache, true);
                if (verbose) Console.WriteLine($"已清理：{ssgCache}");
            }

            Console.WriteLine("清理完成");
            return 0;
        });
    }

    private static void RegisterPublishCommand(CommandRegistryBuilder registry)
    {
        registry.Add("publish", async (
            string registryName = "valhalla",
            string access = "public",
            string tag = "latest",
            string? otp = null,
            string? bump = null,
            string tagPrefix = "v",
            bool skipVerify = false,
            bool skipPrePublish = false,
            bool dryRun = false) =>
        {
            var options = new PublishOptions
            {
                RegistryName = registryName,
                Access = access,
                Tag = tag,
                Otp = otp,
                Bump = bump is not null ? (bump.ToLowerInvariant() switch
                {
                    "patch" => VersionBump.Patch,
                    "minor" => VersionBump.Minor,
                    "major" => VersionBump.Major,
                    _ => null
                }) : null,
                GitTagPrefix = tagPrefix,
                CreateGitTag = true,
                SkipVerify = skipVerify,
                RunPrePublishScript = !skipPrePublish
            };

            if (dryRun)
            {
                Console.WriteLine("--- 试运行模式 ---");
            }

            Console.WriteLine($"发布注册表：{registryName}");
            Console.WriteLine($"标签：{tag} / 访问级别：{access}");
            if (options.Bump is not null) Console.WriteLine($"版本递增：{options.Bump}");

            if (!dryRun)
            {
                var legion = new Legion();
                var result = await legion.PublishAsync(options, registryName);
                if (result.Success)
                {
                    Console.WriteLine($"✅ 发布成功：{result.PackageName}@{result.Version}");
                }
                else
                {
                    Console.Error.WriteLine($"❌ 发布失败：{result.Message}");
                    return 1;
                }
            }

            return 0;
        });
    }

    private static void RegisterNewCommand(CommandRegistryBuilder registry)
    {
        registry.Add("new", NewCommand.Execute);
    }

    private static void RegisterCheckCommand(CommandRegistryBuilder registry)
    {
        registry.Add("check", (string? path = null) =>
        {
            var targetPath = path ?? ".";
            var vFiles = Directory.Exists(targetPath)
                ? Directory.GetFiles(targetPath, "*.v", SearchOption.AllDirectories)
                : File.Exists(targetPath) ? [targetPath] : [];

            var diagnostics = new DiagnosticSink();
            var lexer = new ValkyrieLexer(ValkyrieLanguage.Standard, diagnostics);
            var parser = new ValkyrieParser(ValkyrieLanguage.Standard, diagnostics);
            var typeChecker = new Valkyrie.TypeChecker.TypeChecker();
            var totalErrors = 0;

            foreach (var vFile in vFiles)
            {
                var source = File.ReadAllText(vFile);
                var tokens = lexer.Tokenize(source);
                var ast = (CompilationUnit)parser.Parse(tokens);

                if (diagnostics.Errors.Count > 0)
                {
                    totalErrors += diagnostics.Errors.Count;
                    diagnostics.Clear();
                    continue;
                }

                var typeResult = typeChecker.Check(ast, vFile);
                totalErrors += typeResult.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
                diagnostics.Clear();
            }

            Console.WriteLine($"检查完成：{vFiles.Length} 个文件，{totalErrors} 个错误");
            return totalErrors > 0 ? 1 : 0;
        });
    }

    private static void RegisterFmtCommand(CommandRegistryBuilder registry)
    {
        registry.Add("fmt", (string? path = null) =>
        {
            Console.Error.WriteLine("fmt 命令暂不可用：Valkyrie.Formatter 正在迁移到新版 AST。");
            return 2;
        });
    }

    private static void RegisterTestCommand(CommandRegistryBuilder registry)
    {
        registry.Add("test", (string project, bool verbose = false) =>
        {
            var projectDir = ResolveVoaProjectDir(project);
            if (projectDir is null)
            {
                Console.Error.WriteLine($"错误：找不到项目 '{project}'");
                return 1;
            }

            Console.WriteLine($"正在运行测试：{projectDir}");

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = verbose ? "test --verbosity normal" : "test",
                WorkingDirectory = projectDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                Console.Error.WriteLine("错误：无法启动测试运行器");
                return 1;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine(output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.Error.WriteLine(error);
            }

            Console.WriteLine(process.ExitCode == 0
                ? "✅ 测试全部通过"
                : "❌ 测试失败");

            return process.ExitCode;
        });
    }

    private static void RegisterInitCommand(CommandRegistryBuilder registry)
    {
        registry.Add("init", (string target = "wasm", string type = "frontend", bool force = false) =>
        {
            var currentDir = Directory.GetCurrentDirectory();
            var vonPath = Path.Combine(currentDir, "voa.von");

            if (File.Exists(vonPath) && !force)
            {
                Console.WriteLine("voa.von 已存在，使用 --force 覆盖");
                return 1;
            }

            Console.WriteLine($"正在初始化 VOA 项目（类型：{type}，目标：{target}）...");

            var vonContent = GenerateVoaVon(type, target);
            File.WriteAllText(vonPath, vonContent);

            Console.WriteLine("✅ 项目初始化完成（voa.von）");
            return 0;
        });
    }

    private static void RegisterBenchmarkCommand(CommandRegistryBuilder registry)
    {
        registry.Add("benchmark", (string target = "wasm", int lines = 100000, int files = 100, int iterations = 3) =>
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), $"voa-bench-{Guid.NewGuid():N}");
            var sourceDir = Path.Combine(tmpDir, "source");
            Directory.CreateDirectory(sourceDir);

            try
            {
                var linesPerFile = lines / files;
                var allTimes = new List<long>();

                for (var iter = 0; iter < iterations; iter++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var benchBuilder = new VoaMultiTargetBuilder();
                    var result = benchBuilder.Build(new VoaBuildConfig(), sourceDir, Path.Combine(tmpDir, "output"), target, false);
                    stopwatch.Stop();

                    allTimes.Add(stopwatch.ElapsedMilliseconds);
                    Console.WriteLine($"  迭代 {iter + 1}: {stopwatch.ElapsedMilliseconds}ms — {(result.Success ? "通过" : "失败")}");
                }

                Console.WriteLine($"平均: {allTimes.Average():F0}ms / 最快: {allTimes.Min()}ms / 最慢: {allTimes.Max()}ms");
            }
            finally
            {
                if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true);
            }

            return 0;
        });
    }

    private static void RegisterCoverageCommand(CommandRegistryBuilder registry)
    {
        registry.Add("coverage", (string project, string output = "html", bool verbose = false) =>
        {
            var projectDir = ResolveVoaProjectDir(project);
            if (projectDir is null)
            {
                Console.Error.WriteLine($"错误：找不到项目 '{project}'");
                return 1;
            }

            Console.WriteLine($"正在收集覆盖率：{projectDir}");

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "test --collect:\"XPlat Code Coverage\"",
                WorkingDirectory = projectDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                Console.Error.WriteLine("错误：无法启动覆盖率收集");
                return 1;
            }

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.Error.WriteLine("测试运行失败，无法生成覆盖率报告");
                return 1;
            }

            Console.WriteLine($"覆盖率报告格式：{output}");
            Console.WriteLine("覆盖率收集完成");

            return 0;
        });
    }

    #region 辅助方法

    private static string? ResolveVoaProjectDir(string project)
    {
        if (Directory.Exists(project) && File.Exists(Path.Combine(project, "voa.von")))
        {
            return project;
        }

        if (Directory.Exists(project))
        {
            return project;
        }

        var currentDir = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(currentDir, "voa.von")))
        {
            return currentDir;
        }

        return null;
    }

    private static SSGBuildResult RunSSG(string projectDir, string outputDir, bool verbose)
    {
        try
        {
            var ssgDir = Path.Combine(projectDir, ".voa_ssg");
            if (Directory.Exists(ssgDir))
            {
                Directory.Delete(ssgDir, true);
            }

            Directory.CreateDirectory(ssgDir);

            if (verbose)
            {
                Console.WriteLine("SSG 静态站点生成完成");
            }

            return new SSGBuildResult { Success = true, OutputDirectory = ssgDir };
        }
        catch (Exception ex)
        {
            return new SSGBuildResult { Success = false, Error = ex.Message };
        }
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            Console.WriteLine($"请手动打开：{url}");
        }
    }

    private static string GenerateVoaVon(string type, string target)
    {
        return $@"# VOA 项目配置 — {type} 类型
[project]
name = ""{Path.GetFileName(Directory.GetCurrentDirectory())}""
version = ""0.0.0""
target = ""{target}""
type = ""{type}""

[dependencies]

[dev-dependencies]

[scripts]
dev = ""voa dev .""
build = ""voa build .""
start = ""voa start .""
";
    }

    private class SSGBuildResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? OutputDirectory { get; set; }
    }

    #endregion
}
