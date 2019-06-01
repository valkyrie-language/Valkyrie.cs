using Legion.Registry;
using Legion.Registry.Conda;
using Legion.Registry.Jsr;
using Legion.Registry.Maven;
using Legion.Registry.Npm;
using Legion.Registry.Nuget;

namespace Legion.Auth;

/// <summary>
/// Vendor 管理器，统一管理注册表源的增删查和登录认证
/// </summary>
public class VendorManager
{
    private readonly RegistrySourceManager _sourceManager;
    private readonly VendorAuthStore _authStore;
    private readonly Dictionary<string, IRegistry> _registries;
    private readonly CredentialDiscoveryManager _credentialDiscovery;

    /// <summary>
    /// 创建 Vendor 管理器
    /// </summary>
    /// <param name="sourceManager">注册表源管理器</param>
    /// <param name="authStore">认证令牌存储</param>
    /// <param name="registries">已注册的注册表适配器字典</param>
    public VendorManager(RegistrySourceManager sourceManager, VendorAuthStore authStore, Dictionary<string, IRegistry> registries)
    {
        _sourceManager = sourceManager;
        _authStore = authStore;
        _registries = registries;
        _credentialDiscovery = new CredentialDiscoveryManager();
        _credentialDiscovery.RegisterBuiltinProviders();
    }

    /// <summary>
    /// 获取凭据发现管理器
    /// </summary>
    public CredentialDiscoveryManager CredentialDiscovery => _credentialDiscovery;

    /// <summary>
    /// 加载认证信息
    /// </summary>
    public void Load()
    {
        _authStore.Load();
    }

    /// <summary>
    /// 保存认证信息
    /// </summary>
    public async Task SaveAsync()
    {
        await _authStore.SaveAsync();
    }

    #region Vendor 管理

    /// <summary>
    /// 添加 Vendor（注册表源）
    /// </summary>
    /// <param name="name">Vendor 名称</param>
    /// <param name="endpoint">端点地址</param>
    public void AddVendor(string name, string endpoint)
    {
        _sourceManager.SetEndpoint(name, endpoint);
    }

    /// <summary>
    /// 移除 Vendor（注册表源及其认证信息）
    /// </summary>
    /// <param name="name">Vendor 名称</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveVendor(string name)
    {
        _authStore.RemoveToken(name);
        return _sourceManager.RemoveEndpoint(name);
    }

    /// <summary>
    /// 列出所有已配置的 Vendor 及其状态
    /// </summary>
    /// <returns>Vendor 状态列表</returns>
    public List<VendorStatus> ListVendors()
    {
        var result = new List<VendorStatus>();

        foreach (var (name, endpoint) in _sourceManager.Sources)
        {
            var authInfo = _authStore.GetAuthInfo(name);
            var availableProviders = _credentialDiscovery.GetAvailableProviders(name);

            result.Add(new VendorStatus
            {
                Name = name,
                Endpoint = endpoint,
                IsLoggedIn = authInfo?.IsLoggedIn ?? false,
                CurrentUser = authInfo?.CurrentUser,
                LoggedInAt = authInfo?.LoggedInAt,
                ExpiresAt = authInfo?.ExpiresAt,
                HasOfficialCredentials = availableProviders.Count > 0,
                AvailableCredentialSources = availableProviders
                    .Select(p => p.ProviderName)
                    .ToList()
            });
        }

        return result;
    }

    /// <summary>
    /// 获取指定 Vendor 的状态
    /// </summary>
    /// <param name="name">Vendor 名称</param>
    /// <returns>Vendor 状态，未找到返回 null</returns>
    public VendorStatus? GetVendorStatus(string name)
    {
        string? endpoint = _sourceManager.GetEndpoint(name);

        if (endpoint is null)
        {
            return null;
        }

        var authInfo = _authStore.GetAuthInfo(name);
        var availableProviders = _credentialDiscovery.GetAvailableProviders(name);

        return new VendorStatus
        {
            Name = name,
            Endpoint = endpoint,
            IsLoggedIn = authInfo?.IsLoggedIn ?? false,
            CurrentUser = authInfo?.CurrentUser,
            LoggedInAt = authInfo?.LoggedInAt,
            ExpiresAt = authInfo?.ExpiresAt,
            HasOfficialCredentials = availableProviders.Count > 0,
            AvailableCredentialSources = availableProviders
                .Select(p => p.ProviderName)
                .ToList()
        };
    }

    #endregion

    #region 登录

    /// <summary>
    /// 登录到指定 Vendor
    /// 如果 token 为空，自动尝试从官方工具配置中发现凭据
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <param name="token">认证令牌，为 null 时自动尝试发现</param>
    /// <returns>登录结果</returns>
    public async Task<VendorLoginResult> LoginAsync(string vendorName, string? token = null)
    {
        string? endpoint = _sourceManager.GetEndpoint(vendorName);

        if (endpoint is null)
        {
            return VendorLoginResult.Fail($"Vendor '{vendorName}' 未配置，请先使用 'legion vendor add' 添加");
        }

        IRegistry? registry = GetRegistry(vendorName, endpoint);

        if (registry is null)
        {
            return VendorLoginResult.Fail($"不支持的 Vendor 类型：'{vendorName}'");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            var discoveryResult = await TryDiscoverAndLoginAsync(vendorName, endpoint, registry);

            if (discoveryResult is not null)
            {
                return discoveryResult;
            }

            var availableProviders = _credentialDiscovery.GetAvailableProviders(vendorName);

            if (availableProviders.Count > 0)
            {
                var sources = string.Join("、", availableProviders.Select(p => p.ProviderName));
                return VendorLoginResult.Fail(
                    $"未提供令牌，且从官方工具（{sources}）自动获取失败，请手动传入 --token");
            }

            return VendorLoginResult.Fail(
                "未提供认证令牌且无可用的官方工具凭据，请手动传入 --token");
        }

        return await VerifyAndSaveTokenAsync(vendorName, endpoint, registry, token, "手动输入");
    }

    /// <summary>
    /// 从官方工具刷新凭据（已有存储令牌时刷新）
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>登录结果</returns>
    public async Task<VendorLoginResult> RefreshFromOfficialToolAsync(string vendorName)
    {
        string? endpoint = _sourceManager.GetEndpoint(vendorName);

        if (endpoint is null)
        {
            return VendorLoginResult.Fail($"Vendor '{vendorName}' 未配置");
        }

        IRegistry? registry = GetRegistry(vendorName, endpoint);

        if (registry is null)
        {
            return VendorLoginResult.Fail($"不支持的 Vendor 类型：'{vendorName}'");
        }

        var result = await TryDiscoverAndLoginAsync(vendorName, endpoint, registry);

        return result ?? VendorLoginResult.Fail(
            $"未找到 '{vendorName}' 的官方工具凭据，请手动登录");
    }

    /// <summary>
    /// 退出指定 Vendor 的登录
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>是否成功登出</returns>
    public async Task<VendorLoginResult> LogoutAsync(string vendorName)
    {
        if (!_authStore.IsLoggedIn(vendorName))
        {
            return VendorLoginResult.Fail($"未登录到 '{vendorName}'");
        }

        _authStore.RemoveToken(vendorName);
        await _authStore.SaveAsync();

        return VendorLoginResult.Ok(vendorName, string.Empty, null);
    }

    /// <summary>
    /// 获取已登录 Vendor 的用户信息
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>用户信息</returns>
    public VendorLoginResult WhoAmI(string vendorName)
    {
        var authInfo = _authStore.GetAuthInfo(vendorName);

        if (authInfo is null || !authInfo.IsLoggedIn)
        {
            return VendorLoginResult.Fail($"未登录到 '{vendorName}'，请先使用 'legion vendor login {vendorName}' 登录");
        }

        return VendorLoginResult.Ok(
            vendorName,
            authInfo.CurrentUser ?? "未知用户",
            authInfo.ExpiresAt);
    }

    /// <summary>
    /// 获取 Vendor 的认证令牌（用于自动注入到发布流程）
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>认证令牌，未登录返回 null</returns>
    public string? GetToken(string vendorName)
    {
        return _authStore.GetToken(vendorName);
    }

    /// <summary>
    /// 检查是否已登录指定 Vendor
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>是否已登录</returns>
    public bool IsLoggedIn(string vendorName)
    {
        return _authStore.IsLoggedIn(vendorName);
    }

    #endregion

    #region 私有方法

    private async Task<VendorLoginResult?> TryDiscoverAndLoginAsync(
        string vendorName, string endpoint, IRegistry registry)
    {
        var credential = await _credentialDiscovery.DiscoverAsync(vendorName);

        if (credential is null || string.IsNullOrEmpty(credential.Token))
        {
            return null;
        }

        try
        {
            var result = await VerifyAndSaveTokenAsync(
                vendorName, endpoint, registry,
                credential.Token,
                credential.Source);

            if (result.Success)
            {
                return result;
            }

            return VendorLoginResult.Fail(
                $"官方工具凭据（{credential.Source}）中的令牌验证失败：{result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            return VendorLoginResult.Fail(
                $"官方工具凭据（{credential.Source}）验证异常：{ex.Message}");
        }
    }

    private async Task<VendorLoginResult> VerifyAndSaveTokenAsync(
        string vendorName, string endpoint, IRegistry registry,
        string token, string source)
    {
        var verifyResult = await registry.VerifyTokenAsync(token);

        if (!verifyResult.Valid)
        {
            return VendorLoginResult.Fail($"令牌验证失败：{verifyResult.ErrorMessage ?? "未知错误"}");
        }

        _authStore.SaveToken(
            vendorName,
            endpoint,
            token,
            verifyResult.Username,
            verifyResult.ExpiresAt);

        await _authStore.SaveAsync();

        return new VendorLoginResult
        {
            Success = true,
            VendorName = vendorName,
            Username = verifyResult.Username ?? "未知用户",
            ExpiresAt = verifyResult.ExpiresAt,
            CredentialSource = source
        };
    }

    private IRegistry? GetRegistry(string vendorName, string endpoint)
    {
        if (_registries.TryGetValue(vendorName, out var registry))
        {
            return registry;
        }

        return vendorName.ToLowerInvariant() switch
        {
            "npm" => new NpmRegistry { Endpoint = endpoint },
            "jsr" => new JsrRegistry { Endpoint = endpoint },
            "conda" => new CondaRegistry { Endpoint = endpoint },
            "maven" => new MavenRegistry { Endpoint = endpoint },
            "nuget" => new NuGetRegistry { Endpoint = endpoint },
            _ => null
        };
    }

    #endregion
}