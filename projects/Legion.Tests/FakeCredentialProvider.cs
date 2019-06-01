using Legion;
using Legion.Auth;

namespace Valkyrie.PackageManager.Tests;

/// <summary>
/// 测试用可控制凭据提供器
/// </summary>
public class FakeCredentialProvider : ICredentialProvider
{
    private readonly string _vendorName;
    private readonly string? _token;
    private readonly string _source;

    public string ProviderName => "测试凭据源";
    public string VendorName => _vendorName;
    public int Priority => 5;

    public FakeCredentialProvider(string vendorName, string? token, string source)
    {
        _vendorName = vendorName;
        _token = token;
        _source = source;
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
            Source = _source
        });
    }
}