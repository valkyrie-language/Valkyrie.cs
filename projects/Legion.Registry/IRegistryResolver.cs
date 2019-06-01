namespace Legion.Registry;

/// <summary>
/// 注册表解析器接口，提供对注册表的按名称/类型查找能力
/// </summary>
public interface IRegistryResolver
{
    /// <summary>
    /// 按名称查找注册表
    /// </summary>
    /// <param name="registryName">注册表名称（npm/jsr/conda/maven/nuget/valhalla）</param>
    /// <returns>注册表实例，未找到返回 null</returns>
    IRegistry? GetRegistry(string registryName);

    /// <summary>
    /// 按类型查找注册表
    /// </summary>
    /// <typeparam name="T">注册表具体类型</typeparam>
    /// <returns>注册表实例，未找到返回 null</returns>
    T? GetRegistry<T>() where T : class, IRegistry;

    /// <summary>
    /// 获取所有已注册的注册表
    /// </summary>
    /// <returns>注册表名称到实例的映射</returns>
    IReadOnlyDictionary<string, IRegistry> GetAllRegistries();

    /// <summary>
    /// 获取所有已注册的注册表名称
    /// </summary>
    ISet<string> GetRegistryNames();
}