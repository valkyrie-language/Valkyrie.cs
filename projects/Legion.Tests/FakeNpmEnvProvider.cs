using Legion;
using Legion.Auth;

namespace Valkyrie.PackageManager.Tests;

/// <summary>
/// 测试用伪造 npm 环境变量提供器
/// </summary>
public class FakeNpmEnvProvider : ICredentialProvider
{
    private readonly string _token;

    public string ProviderName => "测试 npm 环境变量";
    public string VendorName => "npm";
    public int Priority => 5;

    public FakeNpmEnvProvider(string token)
    {
        _token = token;
    }

    public bool IsAvailable()
    {
        return !string.IsNullOrEmpty(_token);
    }

    public Task<DiscoveredCredential?> DiscoverAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            return Task.FromResult<DiscoveredCredential?>(null);
        }

        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
        {
            Token = _token,
            Source = "测试 npm 环境变量"
        });
    }
}