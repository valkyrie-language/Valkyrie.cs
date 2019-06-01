using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Legion.Registry;

/// <summary>
/// 注册表解析器的默认实现，通过枚举所有 IRegistry 服务来解析注册表
/// </summary>
public class RegistryResolver : IRegistryResolver
{
    private readonly IReadOnlyDictionary<string, IRegistry> _registries;
    private readonly ILogger _logger;

    /// <summary>
    /// 创建注册表解析器
    /// </summary>
    /// <param name="registries">所有注册表实例的枚举</param>
    /// <param name="logger">可选的日志记录器</param>
    public RegistryResolver(IEnumerable<IRegistry> registries, ILogger<RegistryResolver>? logger = null)
    {
        _registries = registries.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger ?? NullLogger<RegistryResolver>.Instance;
    }

    /// <inheritdoc />
    public IRegistry? GetRegistry(string registryName)
    {
        if (_registries.TryGetValue(registryName, out var registry))
        {
            return registry;
        }

        _logger.LogWarning("未找到注册表: {RegistryName}", registryName);
        return null;
    }

    /// <inheritdoc />
    public T? GetRegistry<T>() where T : class, IRegistry
    {
        foreach (var (_, registry) in _registries)
        {
            if (registry is T typed)
            {
                return typed;
            }
        }

        _logger.LogWarning("未找到注册表类型: {RegistryType}", typeof(T).Name);
        return null;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IRegistry> GetAllRegistries()
    {
        return _registries;
    }

    /// <inheritdoc />
    public ISet<string> GetRegistryNames()
    {
        return _registries.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}