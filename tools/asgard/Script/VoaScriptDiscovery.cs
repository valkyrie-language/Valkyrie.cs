namespace Asgard.CLI.Script;

/// <summary>
///     VOA 脚本自动发现器，扫描项目 script/ 目录下的 .v 文件
///     仅扫描一级目录，脚本名为文件名（不含扩展名）
/// </summary>
public sealed class VoaScriptDiscovery
{
    /// <summary>
    ///     扫描项目目录下的 script/ 目录，发现所有 .v 脚本文件
    /// </summary>
    /// <param name="projectDir">项目根目录</param>
    /// <returns>脚本名到文件路径的映射</returns>
    public Dictionary<string, string> DiscoverScripts(string projectDir)
    {
        var scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var scriptDir = Path.Combine(projectDir, "script");

        if (!Directory.Exists(scriptDir))
        {
            return scripts;
        }

        var vFiles = Directory.GetFiles(scriptDir, "*.v", SearchOption.TopDirectoryOnly);

        foreach (var vFile in vFiles)
        {
            var scriptName = Path.GetFileNameWithoutExtension(vFile);
            if (string.IsNullOrEmpty(scriptName))
            {
                continue;
            }

            scripts[scriptName] = vFile;
        }

        return scripts;
    }
}
