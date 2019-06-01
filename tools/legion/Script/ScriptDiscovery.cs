namespace Legion.CLI.Script;

/// <summary>
/// 脚本自动发现器，扫描项目 script/ 目录下的 .v 文件
/// </summary>
public class ScriptDiscovery
{
    /// <summary>
    /// 发现项目目录下 script/ 目录中的所有 .v 脚本文件
    /// </summary>
    /// <param name="projectDir">项目目录路径</param>
    /// <returns>脚本名称到文件路径的映射</returns>
    public Dictionary<string, string> DiscoverScripts(string projectDir)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var scriptDir = Path.Combine(projectDir, "script");

        if (!Directory.Exists(scriptDir))
        {
            return result;
        }

        foreach (var filePath in Directory.GetFiles(scriptDir, "*.v", SearchOption.TopDirectoryOnly))
        {
            var scriptName = Path.GetFileNameWithoutExtension(filePath);
            result[scriptName] = filePath;
        }

        return result;
    }

    /// <summary>
    /// 检查项目目录下是否存在指定名称的脚本
    /// </summary>
    /// <param name="projectDir">项目目录路径</param>
    /// <param name="scriptName">脚本名称</param>
    /// <returns>是否存在该脚本</returns>
    public bool HasScript(string projectDir, string scriptName)
    {
        var scripts = DiscoverScripts(projectDir);
        return scripts.ContainsKey(scriptName);
    }
}
