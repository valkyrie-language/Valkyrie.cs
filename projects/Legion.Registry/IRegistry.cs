namespace Legion.Registry;

/// <summary>
/// 注册表抽象接口，所有注册表适配器必须实现此接口
/// </summary>
public interface IRegistry : IDisposable
{
    /// <summary>
    /// 注册表名称标识
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 注册表 API 端点地址
    /// </summary>
    string Endpoint { get; set; }

    /// <summary>
    /// 获取包的元数据信息
    /// </summary>
    /// <param name="packageName">包名称</param>
    /// <param name="version">版本号或版本约束，使用 "latest" 获取最新版本</param>
    /// <returns>包元数据</returns>
    Task<Package> GetPackageAsync(string packageName, string version);

    /// <summary>
    /// 搜索注册表中的包
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <returns>匹配的包列表</returns>
    Task<List<Package>> SearchPackagesAsync(string query);

    /// <summary>
    /// 发布包到注册表
    /// </summary>
    /// <param name="options">发布选项</param>
    /// <param name="tarballData">打包后的二进制数据</param>
    /// <returns>发布结果</returns>
    Task<PublishResult> PublishPackageAsync(PublishOptions options, byte[] tarballData);

    /// <summary>
    /// 下载包的分发文件到指定目录并解压
    /// </summary>
    /// <param name="package">要下载的包元数据（需包含 DistTarball）</param>
    /// <param name="targetDirectory">目标目录</param>
    /// <returns>解压后的文件根目录路径</returns>
    Task<string> DownloadPackageAsync(Package package, string targetDirectory);

    /// <summary>
    /// 获取包的所有可用版本列表
    /// </summary>
    /// <param name="packageName">包名称</param>
    /// <returns>版本号列表</returns>
    Task<List<string>> GetPackageVersionsAsync(string packageName);

    /// <summary>
    /// 验证认证令牌是否有效
    /// </summary>
    /// <param name="token">认证令牌</param>
    /// <returns>验证结果，包含用户名等信息</returns>
    Task<TokenVerifyResult> VerifyTokenAsync(string token);

    /// <summary>
    /// 检查指定版本的包是否存在于注册表中
    /// </summary>
    /// <param name="packageName">包名称</param>
    /// <param name="version">版本号</param>
    /// <returns>是否存在</returns>
    async Task<bool> PackageExistsAsync(string packageName, string version)
    {
        try
        {
            var versions = await GetPackageVersionsAsync(packageName);
            return versions.Contains(version);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取包的最新稳定版本号
    /// </summary>
    /// <param name="packageName">包名称</param>
    /// <returns>最新版本号，未找到返回 null</returns>
    async Task<string?> GetLatestVersionAsync(string packageName)
    {
        try
        {
            var pkg = await GetPackageAsync(packageName, "latest");
            return pkg.Version;
        }
        catch
        {
            return null;
        }
    }
}