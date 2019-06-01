using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using Legion.Registry;
using Legion.Scripts;
using Legion.Tools;
using Legion.Version;

namespace Legion.Package;

/// <summary>
///     包发布器，负责打包、版本递增、Git Tag 和发布流程
/// </summary>
public class PackagePublisher
{
    private readonly Dictionary<string, IRegistry> _registries;

    public PackagePublisher(Dictionary<string, IRegistry> registries)
    {
        _registries = registries;
    }

    /// <summary>
    ///     执行完整发布流程：版本递增 → Git 检查 → 打包 → 发布 → Tag
    /// </summary>
    public async Task<PublishResult> PublishAsync(PublishOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PackageName))
        {
            throw new ArgumentException("包名不能为空");
        }

        if (string.IsNullOrWhiteSpace(options.PackagePath))
        {
            throw new ArgumentException("包路径不能为空");
        }

        if (!Directory.Exists(options.PackagePath))
        {
            throw new DirectoryNotFoundException($"包目录不存在: {options.PackagePath}");
        }

        // 1. 版本递增
        if (options.Bump is not null)
        {
            options.Version = BumpVersion(options.PackagePath, options.Version, options.Bump.Value);
            Console.WriteLine($"版本已递增 → {options.Version}");
        }

        if (string.IsNullOrWhiteSpace(options.Version))
        {
            throw new ArgumentException("版本号不能为空");
        }

        // 2. Git 工作区检查
        if (!options.SkipGitCheck)
        {
            var (gitClean, gitMessage) = CheckGitStatus(options.PackagePath);
            if (!gitClean)
            {
                Console.WriteLine("⚠ Git 工作区不干净，建议先提交或使用 --skip-git-check：");
                Console.WriteLine($"  {gitMessage}");

                if (!options.CreateGitTag)
                {
                }
            }
        }

        // 3. 包验证
        if (!ValidatePackage(options))
        {
            return new PublishResult
            {
                Success = false,
                PackageName = options.PackageName,
                Version = options.Version,
                Message = "包验证失败"
            };
        }

        // 4. 发布前脚本
        if (options.RunPrePublishScript)
        {
            await RunPrePublishScript(options.PackagePath);
        }

        // 5. 打包
        var ignore = new LegionIgnore(options.PackagePath);
        string ignoreFilePath = Path.Combine(options.PackagePath, "legion.ignore");
        if (File.Exists(ignoreFilePath))
        {
            ignore.Load();
        }

        var packResult = PackagePacker.Pack(options.PackagePath, ignore);
        byte[] tarballData = packResult.TarballData;
        Console.WriteLine($"打包完成: {options.PackageName}@{options.Version} " +
                          $"({packResult.Size / 1024.0:F1} KB, {packResult.FileCount} 个文件, {packResult.Sha256[..24]}...)");

        // 6. 发布
        if (!_registries.TryGetValue(options.RegistryName, out var registry))
        {
            throw new ArgumentException($"注册器 {options.RegistryName} 未找到");
        }

        Console.WriteLine($"正在发布到 {options.RegistryName} ({registry.Endpoint})...");
        var result = await registry.PublishPackageAsync(options, tarballData);

        // 7. Git Tag
        if (result.Success && options.CreateGitTag)
        {
            var tagResult = CreateGitTag(options.PackagePath, options.Version, options.GitTagPrefix);
            if (tagResult.Success)
            {
                Console.WriteLine($"Git Tag 已创建：{tagResult.TagName}");

                if (tagResult.Pushed)
                {
                    Console.WriteLine("Tag 已推送到远程仓库");
                }
            }
            else
            {
                Console.WriteLine($"Git Tag 创建失败：{tagResult.Error}");
            }
        }

        return result;
    }

    #region 版本递增

    /// <summary>
    ///     按指定类型递增版本号
    /// </summary>
    /// <param name="packagePath">包路径（用于写回 legion.von）</param>
    /// <param name="currentVersion">当前版本号字符串</param>
    /// <param name="bump">递增类型</param>
    /// <returns>新版本号字符串</returns>
    public static string BumpVersion(string packagePath, string currentVersion, VersionBump bump)
    {
        if (!SemanticVersion.TryParse(currentVersion, out var semVer) || semVer is null)
        {
            throw new ArgumentException($"无效的版本号：{currentVersion}");
        }

        SemanticVersion newVersion = bump switch
        {
            VersionBump.Patch => new SemanticVersion(semVer.Major, semVer.Minor, semVer.Patch + 1),
            VersionBump.Minor => new SemanticVersion(semVer.Major, semVer.Minor + 1, 0),
            VersionBump.Major => new SemanticVersion(semVer.Major + 1, 0, 0),
            _ => semVer
        };

        var newVersionStr = newVersion.ToString() ?? "0.0.0";
        UpdateVersionInManifest(packagePath, newVersionStr);
        return newVersionStr;
    }

    /// <summary>
    ///     写回 legion.von 中的版本号
    /// </summary>
    private static void UpdateVersionInManifest(string packagePath, string newVersion)
    {
        var manifestPath = Path.Combine(packagePath, "legion.von");

        if (!File.Exists(manifestPath))
        {
            return;
        }

        var manifest = new LegionManifest(packagePath);
        manifest.Load();
        manifest.Version = newVersion;
        manifest.Save();
    }

    #endregion

    #region Git 集成

    /// <summary>
    ///     检查 Git 工作区状态
    /// </summary>
    /// <returns>(是否干净, 状态信息)</returns>
    public static (bool IsClean, string Message) CheckGitStatus(string packagePath)
    {
        try
        {
            var status = RunGitCommand(packagePath, "status --porcelain");
            var isClean = string.IsNullOrWhiteSpace(status.StdOut);

            if (!isClean)
            {
                var changedFiles = status.StdOut?.Trim().Split('\n').Length ?? 0;
                return (false, $"{changedFiles} 个文件有改动（git status --porcelain）");
            }

            return (true, "工作区干净");
        }
        catch
        {
            return (true, "无法检测 Git 状态（未安装 Git 或非 Git 仓库）");
        }
    }

    /// <summary>
    ///     创建 Git Tag 并尝试推送
    /// </summary>
    public static GitTagResult CreateGitTag(string packagePath, string version, string? prefix)
    {
        var tagName = $"{prefix ?? string.Empty}{version}";

        try
        {
            var addResult = RunGitCommand(packagePath, $"tag -a \"{tagName}\" -m \"Release {version}\"");
            if (addResult.ExitCode != 0)
            {
                return new GitTagResult { Success = false, Error = addResult.StdErr };
            }

            var pushResult = RunGitCommand(packagePath, "push origin --tags");
            return new GitTagResult
            {
                Success = true,
                TagName = tagName,
                Pushed = pushResult.ExitCode == 0
            };
        }
        catch (Exception ex)
        {
            return new GitTagResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    ///     获取当前分支名
    /// </summary>
    public static string? GetGitBranch(string packagePath)
    {
        try
        {
            var result = RunGitCommand(packagePath, "rev-parse --abbrev-ref HEAD");
            return result.ExitCode == 0 ? result.StdOut?.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private static GitCommandResult RunGitCommand(string workingDir, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(TimeSpan.FromSeconds(10).Milliseconds);

        return new GitCommandResult
        {
            ExitCode = process.ExitCode,
            StdOut = stdout,
            StdErr = stderr
        };
    }

    private struct GitCommandResult
    {
        public int ExitCode;
        public string? StdOut;
        public string? StdErr;
    }

    /// <summary>
    ///     Git Tag 操作结果
    /// </summary>
    public class GitTagResult
    {
        public bool Success { get; set; }
        public string? TagName { get; set; }
        public bool Pushed { get; set; }
        public string? Error { get; set; }
    }

    #endregion

    #region 发布前脚本

    /// <summary>
    ///     执行发布前脚本（legion.von 中 scripts.prePublish）
    /// </summary>
    private async Task RunPrePublishScript(string packagePath)
    {
        var scriptRunner = new ScriptRunner(packagePath);

        try
        {
            var manifest = new LegionManifest(packagePath);
            manifest.Load();

            if (manifest.Scripts.TryGetValue("prePublish", out var script))
            {
                Console.WriteLine("执行 prePublish 脚本...");
                await scriptRunner.RunAsync(script);
            }
        }
        catch
        {
            Console.WriteLine("prePublish 脚本执行失败，继续发布...");
        }
    }

    #endregion

    public bool ValidatePackage(PublishOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PackageName))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.Version))
        {
            return false;
        }

        if (!SemanticVersion.TryParse(options.Version, out _))
        {
            return false;
        }

        if (!Directory.Exists(options.PackagePath))
        {
            return false;
        }

        return true;
    }

    public async Task<bool> CheckPackageExistsAsync(string packageName, string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
        {
            return false;
        }

        try
        {
            var package = await registry.GetPackageAsync(packageName, "latest");
            return package is not null;
        }
        catch (RegistryException ex) when (ex.StatusCode == 404)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public byte[] CreateTarball(string packagePath, string packageName, string version)
    {
        var ignore = new LegionIgnore(packagePath);
        string ignoreFilePath = Path.Combine(packagePath, "legion.ignore");
        if (File.Exists(ignoreFilePath))
        {
            ignore.Load();
        }

        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            using var tarWriter = new TarWriter(gzipStream, leaveOpen: true);

            foreach (var filePath in Directory.EnumerateFiles(packagePath, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(packagePath, filePath);

                if (ShouldIgnoreFile(relativePath, ignore))
                {
                    continue;
                }

                string entryPath = $"package/{relativePath.Replace('\\', '/')}";

                var fileInfo = new FileInfo(filePath);
                var entry = new PaxTarEntry(TarEntryType.RegularFile, entryPath)
                {
                    ModificationTime = fileInfo.LastWriteTimeUtc
                };

                using (var fileStream = fileInfo.OpenRead())
                {
                    entry.DataStream = fileStream;
                    tarWriter.WriteEntry(entry);
                }
            }
        }

        return memoryStream.ToArray();
    }

    private bool ShouldIgnoreFile(string relativePath, LegionIgnore ignore)
    {
        string normalizedPath = relativePath.Replace('\\', '/');

        if (normalizedPath.StartsWith("vendors/") || normalizedPath.StartsWith("vendors\\"))
        {
            return true;
        }

        if (normalizedPath.StartsWith(".cache/") || normalizedPath.StartsWith(".cache\\"))
        {
            return true;
        }

        if (normalizedPath.StartsWith("node_modules/") || normalizedPath.StartsWith("node_modules\\"))
        {
            return true;
        }

        if (normalizedPath == "legion-lock.von")
        {
            return true;
        }

        if (ignore.IsIgnored(normalizedPath))
        {
            return true;
        }

        return false;
    }

    public static string ComputeIntegrity(byte[] data)
    {
        using var sha512 = SHA512.Create();
        byte[] hash = sha512.ComputeHash(data);
        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    public static string ComputeFileIntegrity(string filePath)
    {
        using var sha512 = SHA512.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hash = sha512.ComputeHash(stream);
        return $"sha512-{Convert.ToBase64String(hash)}";
    }
}