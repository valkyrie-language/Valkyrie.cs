namespace Valkyrie.CLI.Module;

/// <summary>
/// 模块路径解析器
/// 按优先级依次在当前文件目录、项目根目录、vendors/ 目录和全局 vendors/ 目录中查找模块
/// </summary>
public sealed class ModuleResolver
{
    /// <summary>
    /// 解析模块路径
    /// 按以下顺序查找：
    /// 1. 当前文件所在目录
    /// 2. 项目根目录
    /// 3. 项目 vendors/ 目录（仅直接子项，非递归）
    /// 4. 全局 vendors/ 目录（VALKYRIE_HOME 环境变量或 ~/.valkyrie/vendors/）
    /// </summary>
    /// <param name="moduleName">模块名称，如 some-package</param>
    /// <param name="currentFilePath">当前源文件路径</param>
    /// <param name="projectRoot">项目根目录</param>
    /// <returns>解析到的模块目录或文件路径，未找到返回 null</returns>
    public string? ResolveModule(string moduleName, string currentFilePath, string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return null;
        }

        var searchPaths = BuildSearchPaths(currentFilePath, projectRoot);

        foreach (var searchPath in searchPaths)
        {
            var resolved = TryResolveInPath(moduleName, searchPath);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        return null;
    }

    #region 搜索路径构建

    /// <summary>
    /// 构建模块搜索路径列表，按优先级排列
    /// </summary>
    /// <param name="currentFilePath">当前源文件路径</param>
    /// <param name="projectRoot">项目根目录</param>
    /// <returns>搜索路径列表</returns>
    private static List<string> BuildSearchPaths(string currentFilePath, string projectRoot)
    {
        var paths = new List<string>();

        var currentDir = Path.GetDirectoryName(currentFilePath);
        if (!string.IsNullOrEmpty(currentDir))
        {
            paths.Add(currentDir);
        }

        if (!string.IsNullOrEmpty(projectRoot))
        {
            paths.Add(projectRoot);

            var projectVendors = Path.Combine(projectRoot, "vendors");
            if (Directory.Exists(projectVendors))
            {
                paths.Add(projectVendors);
            }
        }

        var globalVendors = ResolveGlobalVendorsPath();
        if (globalVendors is not null && !paths.Contains(globalVendors))
        {
            paths.Add(globalVendors);
        }

        return paths;
    }

    /// <summary>
    /// 解析全局 vendors/ 目录路径
    /// 优先使用 VALKYRIE_HOME 环境变量，否则使用 ~/.valkyrie/vendors/
    /// </summary>
    /// <returns>全局 vendors 目录路径，无法确定时返回 null</returns>
    private static string? ResolveGlobalVendorsPath()
    {
        var valkyrieHome = Environment.GetEnvironmentVariable("VALKYRIE_HOME");
        if (!string.IsNullOrEmpty(valkyrieHome))
        {
            var vendorsPath = Path.Combine(valkyrieHome, "vendors");
            if (Directory.Exists(vendorsPath))
            {
                return vendorsPath;
            }
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(homeDir))
        {
            var defaultVendors = Path.Combine(homeDir, ".valkyrie", "vendors");
            if (Directory.Exists(defaultVendors))
            {
                return defaultVendors;
            }
        }

        return null;
    }

    #endregion

    #region 路径解析

    /// <summary>
    /// 在指定搜索路径中尝试解析模块
    /// 对于 vendors/ 目录，仅查找直接子项（非递归）
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="searchPath">搜索路径</param>
    /// <returns>解析到的路径，未找到返回 null</returns>
    private static string? TryResolveInPath(string moduleName, string searchPath)
    {
        var moduleDir = Path.Combine(searchPath, moduleName);
        if (Directory.Exists(moduleDir))
        {
            return moduleDir;
        }

        var moduleFile = Path.Combine(searchPath, $"{moduleName}.v");
        if (File.Exists(moduleFile))
        {
            return moduleFile;
        }

        return null;
    }

    #endregion
}
