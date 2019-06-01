using Nyar.Types;

namespace Legion.CLI.Target;

/// <summary>
/// 统一目标三元组解析器，支持短别名和完整三元组解析
/// </summary>
public class TargetTripleResolver
{
    /// <summary>
    /// 短别名到完整目标三元组的映射
    /// </summary>
    private static readonly Dictionary<string, string> ShortAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nyar"] = "nyar-unknown-native",
        ["gnosis"] = "gnosis-unknown-native",
        ["wasm"] = "wasm32-unknown-web",
        ["wasip1"] = "wasm32-unknown-wasi-wasip1",
        ["wasip2"] = "wasm32-unknown-wasi-wasip2",
        ["clr"] = "clr-microsoft-windows",
        ["jvm"] = "jvm-openjdk-linux",
        ["native"] = "x86_64-pc-windows-msvc"
    };

    /// <summary>
    /// 解析目标字符串为编译目标，支持短别名和完整三元组
    /// </summary>
    /// <param name="target">目标字符串，可以是短别名或完整三元组</param>
    /// <returns>解析后的编译目标，解析失败返回 null</returns>
    public CompilationTarget? Resolve(string target)
    {
        if (TargetTriple.TryParse(target, out var triple))
        {
            return triple.ToCompilationTarget();
        }

        return null;
    }

    /// <summary>
    /// 获取所有短别名及其对应的三元组字符串
    /// </summary>
    /// <returns>短别名到三元组字符串的映射</returns>
    public Dictionary<string, string> GetAllShortAliases()
    {
        return new Dictionary<string, string>(ShortAliases, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查目标字符串是否为受支持的编译目标
    /// </summary>
    /// <param name="target">目标字符串</param>
    /// <returns>是否为受支持的编译目标</returns>
    public bool IsSupportedTarget(string target)
    {
        return TargetTriple.TryParse(target, out _);
    }
}
