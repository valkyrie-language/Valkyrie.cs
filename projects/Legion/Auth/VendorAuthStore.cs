using Legion.Tools;
using Oak.Data;
using Oak.Von;

namespace Legion.Auth;

/// <summary>
/// Vendor 认证令牌存储，管理各注册表的登录状态和令牌持久化
/// </summary>
public class VendorAuthStore
{
    private readonly string _authFilePath;
    private readonly Dictionary<string, VendorAuthInfo> _authInfos = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 创建认证存储实例
    /// </summary>
    /// <param name="configDirectory">配置目录（通常为 ~/.valkyrie/）</param>
    public VendorAuthStore(string configDirectory)
    {
        _authFilePath = Path.Combine(configDirectory, "auth.von");
    }

    /// <summary>
    /// 获取所有认证信息
    /// </summary>
    public IReadOnlyDictionary<string, VendorAuthInfo> All => _authInfos;

    /// <summary>
    /// 从文件加载认证信息
    /// </summary>
    public void Load()
    {
        if (!File.Exists(_authFilePath))
        {
            return;
        }

        try
        {
            string content = File.ReadAllText(_authFilePath);

            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var parser = new GonParser();
            var value = parser.Deserialize(content);

            if (value.Type == SerdeValueType.Object && value.Fields is not null)
            {
                foreach (var field in value.Fields)
                {
                    string vendorName = field.Key;
                    var info = ParseAuthInfo(field.Value);

                    if (info is not null)
                    {
                        info.VendorName = vendorName;
                        _authInfos[vendorName] = info;
                    }
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// 保存认证信息到文件
    /// </summary>
    public async Task SaveAsync()
    {
        string? dir = Path.GetDirectoryName(_authFilePath);

        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var vendorFields = new Dictionary<string, SerdeValue>();

        foreach (var (name, info) in _authInfos)
        {
            if (info.IsLoggedIn)
            {
                vendorFields[name] = SerializeAuthInfo(info);
            }
        }

        var root = SerdeValue.Object(vendorFields);
        string content = VonFormatter.Format(root);
        await File.WriteAllTextAsync(_authFilePath, content);
    }

    /// <summary>
    /// 保存 Vendor 认证令牌
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <param name="endpoint">注册表端点</param>
    /// <param name="token">认证令牌</param>
    /// <param name="currentUser">当前用户名</param>
    /// <param name="expiresAt">过期时间</param>
    public void SaveToken(string vendorName, string endpoint, string token, string? currentUser = null, DateTime? expiresAt = null)
    {
        _authInfos[vendorName] = new VendorAuthInfo
        {
            VendorName = vendorName,
            Endpoint = endpoint,
            Token = token,
            CurrentUser = currentUser,
            LoggedInAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// 获取 Vendor 的认证令牌
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>认证令牌，未登录或已过期返回 null</returns>
    public string? GetToken(string vendorName)
    {
        if (_authInfos.TryGetValue(vendorName, out var info) && info.IsLoggedIn)
        {
            return info.Token;
        }

        return null;
    }

    /// <summary>
    /// 获取 Vendor 认证信息
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>认证信息，未找到返回 null</returns>
    public VendorAuthInfo? GetAuthInfo(string vendorName)
    {
        return _authInfos.TryGetValue(vendorName, out var info) ? info : null;
    }

    /// <summary>
    /// 移除 Vendor 认证令牌（登出）
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveToken(string vendorName)
    {
        return _authInfos.Remove(vendorName);
    }

    /// <summary>
    /// 检查是否已登录指定 Vendor
    /// </summary>
    /// <param name="vendorName">Vendor 名称</param>
    /// <returns>是否已登录且令牌未过期</returns>
    public bool IsLoggedIn(string vendorName)
    {
        return _authInfos.TryGetValue(vendorName, out var info) && info.IsLoggedIn;
    }

    /// <summary>
    /// 生成令牌的混淆存储字符串（基础保护）
    /// </summary>
    /// <param name="token">原始令牌</param>
    /// <returns>混淆后的令牌</returns>
    public static string ObfuscateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return token;
        }

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(token);

        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] ^= 0x55;
        }

        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 反混淆令牌
    /// </summary>
    /// <param name="obfuscated">混淆后的令牌</param>
    /// <returns>原始令牌</returns>
    public static string DeobfuscateToken(string obfuscated)
    {
        if (string.IsNullOrEmpty(obfuscated))
        {
            return obfuscated;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(obfuscated);

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= 0x55;
            }

            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return obfuscated;
        }
    }

    private static VendorAuthInfo? ParseAuthInfo(SerdeValue value)
    {
        if (value.Type != SerdeValueType.Object || value.Fields is null)
        {
            return null;
        }

        var info = new VendorAuthInfo();

        var endpointField = value.GetField("endpoint");
        if (endpointField is not null)
        {
            info.Endpoint = endpointField.GetString() ?? string.Empty;
        }

        var tokenField = value.GetField("token");
        if (tokenField is not null)
        {
            string token = tokenField.GetString() ?? string.Empty;
            info.Token = DeobfuscateToken(token);
        }

        var userField = value.GetField("user");
        if (userField is not null)
        {
            info.CurrentUser = userField.GetString();
        }

        var loginField = value.GetField("loggedInAt");
        if (loginField is not null && DateTime.TryParse(loginField.GetString(), out var loginTime))
        {
            info.LoggedInAt = loginTime;
        }

        var expiryField = value.GetField("expiresAt");
        if (expiryField is not null && DateTime.TryParse(expiryField.GetString(), out var expiryTime))
        {
            info.ExpiresAt = expiryTime;
        }

        return info;
    }

    private static SerdeValue SerializeAuthInfo(VendorAuthInfo info)
    {
        var fields = new Dictionary<string, SerdeValue>
        {
            ["endpoint"] = SerdeValue.String(info.Endpoint),
            ["token"] = SerdeValue.String(ObfuscateToken(info.Token)),
            ["loggedInAt"] = SerdeValue.String(info.LoggedInAt.ToString("O"))
        };

        if (info.CurrentUser is not null)
        {
            fields["user"] = SerdeValue.String(info.CurrentUser);
        }

        if (info.ExpiresAt.HasValue)
        {
            fields["expiresAt"] = SerdeValue.String(info.ExpiresAt.Value.ToString("O"));
        }

        return SerdeValue.Object(fields);
    }
}