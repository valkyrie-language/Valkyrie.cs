# 注册表配置

Legion 通过 `IRegistry` 接口适配多种注册表，对外提供统一的依赖解析体验。

## 内置注册表

| 注册表 | 实现类 | 默认端点 |
|:---|:---|:---|
| npm | `NpmRegistry` | `https://registry.npmjs.org` |
| jsr | `JsrRegistry` | `https://jsr.io` |
| conda | `CondaRegistry` | `https://api.anaconda.org` |
| NuGet | `NuGetRegistry` | `https://api.nuget.org/v3` |
| Maven Central | `MavenRegistry` | `https://search.maven.org` |
| Valhalla | `ValhallaRegistry` | `https://valhalla.nyar.dev` |

## 认证互通

**Legion 的登录与各注册表的官方 CLI 工具登录状态完全互通。** 无论用官方工具登录还是用 Legion 登录，另一方都可以直接使用已验证的凭据。

```
npm login        ←→ legion login      （共享 ~/.npmrc）
deno login       ←→ legion login      （共享 deno/config.json）
anaconda login   ←→ legion login      （共享 nucleus/tokens.json）
nuget setapikey  ←→ legion login      （共享 NuGet.Config）
maven settings   ←→ legion login      （共享 ~/.m2/settings.xml）
```

**只需登录一次，所有工具都能用。**

## 自动凭据发现

`legion login` 自动按优先级顺序尝试发现凭据：

1. **优先级 10** — 官方 CLI 配置文件（如 `.npmrc`、`settings.xml`）
2. **优先级 20** — 环境变量（如 `NODE_AUTH_TOKEN`、`NUGET_API_KEY`）
3. **优先级 100** — 手动输入的令牌

```bash
# 如果已通过 npm CLI 登录过，直接运行
legion login
# → 自动从 ~/.npmrc 发现令牌 → 验证通过 → 登录成功
```

## 认证命令

```bash
legion vendor login <name>              # 登录（自动凭据发现）
legion vendor login <name> --token <t>  # 登录（手动令牌）
legion vendor logout <name>             # 退出登录
legion vendor whoami <name>             # 查看当前用户
legion vendor list                      # 列出所有 Vendor
legion vendor refresh <name>            # 刷新凭据
```

## 令牌存储

登录成功后令牌安全存储在 `~/.valkyrie/auth.von`：

```
{
    npm: {
        endpoint: "https://registry.npmjs.org",
        token: "***已混淆***",
        user: "your-username",
        loggedInAt: "2026-05-01T12:00:00Z"
    }
}
```

## 注册表源配置

`~/.valkyrie/registry-sources.von` 存储注册表端点：

```
{
    npm: "https://registry.npmjs.org",
    jsr: "https://jsr.io",
    conda: "https://api.anaconda.org",
    maven: "https://search.maven.org",
    nuget: "https://api.nuget.org/v3",
    valhalla: "https://valhalla.nyar.dev"
}
```

## 指定注册表

依赖声明中可指定注册表：

```
[dependencies]
my-package = { version = "1.0.0", registry = "npm" }
```

未指定时使用默认注册表。

## 自定义注册表

实现 `IRegistry` 接口即可添加：

```csharp
public class MyCustomRegistry : IRegistry
{
    public string Name => "my-registry";
    public string Endpoint { get; set; } = "https://my.example.com";

    public async Task<Package> GetPackageAsync(string name, string version) { /* ... */ }
    public async Task<List<Package>> SearchPackagesAsync(string query) { /* ... */ }
    public async Task<PublishResult> PublishPackageAsync(PublishOptions opts, byte[] data) { /* ... */ }
    public async Task<string> DownloadPackageAsync(Package pkg, string dir) { /* ... */ }
    public async Task<List<string>> GetPackageVersionsAsync(string name) { /* ... */ }
    public async Task<TokenVerifyResult> VerifyTokenAsync(string token) { /* ... */ }
}

legion.RegisterRegistry(new MyCustomRegistry());
```
