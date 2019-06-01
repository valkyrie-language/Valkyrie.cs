using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Valhalla.Server.Storage;

/// <summary>
/// S3 兼容存储后端 — 基于 HTTP REST API 实现，不依赖 AWS SDK
/// 支持 AWS S3、MinIO、Cloudflare R2 等兼容存储
/// </summary>
public class S3Storage : IStorage
{
    private readonly string _bucketName;
    private readonly string _endpoint;
    private readonly string _region;
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 创建 S3 存储后端（公开读写模式，无签名）
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="endpoint">S3 端点地址</param>
    /// <param name="region">区域</param>
    public S3Storage(string bucketName, string endpoint, string region)
        : this(bucketName, endpoint, region, string.Empty, string.Empty)
    {
    }

    /// <summary>
    /// 创建 S3 存储后端（带签名认证）
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="endpoint">S3 端点地址</param>
    /// <param name="region">区域</param>
    /// <param name="accessKey">Access Key</param>
    /// <param name="secretKey">Secret Key</param>
    public S3Storage(string bucketName, string endpoint, string region, string accessKey, string secretKey)
    {
        _bucketName = bucketName;
        _endpoint = endpoint.TrimEnd('/');
        _region = region;
        _accessKey = accessKey;
        _secretKey = secretKey;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    /// <inheritdoc />
    public async Task<string?> ReadStringAsync(string path, CancellationToken ct = default)
    {
        var response = await SendRequestAsync(HttpMethod.Get, path, ct: ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <inheritdoc />
    public async Task<byte[]?> ReadBytesAsync(string path, CancellationToken ct = default)
    {
        var response = await SendRequestAsync(HttpMethod.Get, path, ct: ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    /// <inheritdoc />
    public async Task<StorageResult> WriteStringAsync(string path, string content, CancellationToken ct = default)
    {
        try
        {
            var data = Encoding.UTF8.GetBytes(content);
            var response = await SendRequestAsync(HttpMethod.Put, path, data, "text/plain; charset=utf-8", ct);
            response.EnsureSuccessStatusCode();
            return StorageResult.Succeed();
        }
        catch (Exception ex)
        {
            return StorageResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<StorageResult> WriteBytesAsync(string path, byte[] data, CancellationToken ct = default)
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Put, path, data, "application/octet-stream", ct);
            response.EnsureSuccessStatusCode();
            return StorageResult.Succeed();
        }
        catch (Exception ex)
        {
            return StorageResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        var response = await SendRequestAsync(HttpMethod.Head, path, ct: ct);
        return response.StatusCode == HttpStatusCode.OK;
    }

    /// <inheritdoc />
    public async Task<StorageResult> DeleteAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Delete, path, ct: ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return StorageResult.Succeed();
            }

            response.EnsureSuccessStatusCode();
            return StorageResult.Succeed();
        }
        catch (Exception ex)
        {
            return StorageResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> ListAsync(string prefix, CancellationToken ct = default)
    {
        var result = new List<string>();
        string? continuationToken = null;

        do
        {
            string query = $"list-type=2&prefix={Uri.EscapeDataString(prefix)}&max-keys=1000";

            if (continuationToken is not null)
            {
                query += $"&continuation-token={Uri.EscapeDataString(continuationToken)}";
            }

            var response = await SendRequestAsync(HttpMethod.Get, $"?{query}", ct: ct);

            if (!response.IsSuccessStatusCode)
            {
                break;
            }

            string xmlContent = await response.Content.ReadAsStringAsync(ct);
            var doc = XDocument.Parse(xmlContent);

            XNamespace ns = "http://s3.amazonaws.com/doc/2006-03-01/";

            foreach (var content in doc.Root?.Elements(ns + "Contents") ?? Enumerable.Empty<XElement>())
            {
                var keyElement = content.Element(ns + "Key");
                if (keyElement is not null)
                {
                    result.Add(keyElement.Value);
                }
            }

            var isTruncated = doc.Root?.Element(ns + "IsTruncated")?.Value == "true";
            continuationToken = isTruncated
                ? doc.Root?.Element(ns + "NextContinuationToken")?.Value
                : null;
        } while (continuationToken is not null);

        return result;
    }

    #region S3 签名

    /// <summary>
    /// 发送 S3 请求（自动签名）
    /// </summary>
    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpMethod method,
        string path,
        byte[]? data = null,
        string? contentType = null,
        CancellationToken ct = default)
    {
        string normalizedPath = path.Replace('\\', '/').TrimStart('/');
        string url = $"{_endpoint}/{_bucketName}/{normalizedPath}";

        using var request = new HttpRequestMessage(method, url);

        if (data is not null)
        {
            request.Content = new ByteArrayContent(data);
        }

        string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        string date = timestamp.Substring(0, 8);

        request.Headers.Add("Host", new Uri(url).Host);
        request.Headers.Add("x-amz-date", timestamp);
        request.Headers.Add("x-amz-content-sha256", ComputeSha256Hex(data ?? Array.Empty<byte>()));

        if (contentType is not null && data is not null)
        {
            request.Content!.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        if (!string.IsNullOrEmpty(_accessKey) && !string.IsNullOrEmpty(_secretKey))
        {
            SignRequest(request, method, normalizedPath, timestamp, date, data);
        }

        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    /// <summary>
    /// AWS Signature Version 4 签名
    /// </summary>
    private void SignRequest(
        HttpRequestMessage request,
        HttpMethod method,
        string path,
        string timestamp,
        string date,
        byte[]? data)
    {
        string service = "s3";
        string credentialScope = $"{date}/{_region}/{service}/aws4_request";

        var signedHeaders = new List<string> { "host", "x-amz-content-sha256", "x-amz-date" };
        var canonicalHeaders = new StringBuilder();
        canonicalHeaders.Append($"host:{request.Headers.Host}\n");
        canonicalHeaders.Append($"x-amz-content-sha256:{request.Headers.GetValues("x-amz-content-sha256").First()}\n");
        canonicalHeaders.Append($"x-amz-date:{timestamp}\n");

        string signedHeadersStr = string.Join(";", signedHeaders);

        string payloadHash = request.Headers.GetValues("x-amz-content-sha256").First();

        string canonicalRequest = $"{method}\n/{_bucketName}/{path}\n\n{canonicalHeaders}\n{signedHeadersStr}\n{payloadHash}";

        string stringToSign = $"AWS4-HMAC-SHA256\n{timestamp}\n{credentialScope}\n{ComputeSha256Hex(Encoding.UTF8.GetBytes(canonicalRequest))}";

        byte[] signingKey = DeriveSigningKey(date, _region, service, _secretKey);
        string signature = ComputeHmacHex(signingKey, Encoding.UTF8.GetBytes(stringToSign));

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "AWS4-HMAC-SHA256",
            $"Credential={_accessKey}/{credentialScope}, SignedHeaders={signedHeadersStr}, Signature={signature}");
    }

    /// <summary>
    /// 派生签名密钥
    /// </summary>
    private static byte[] DeriveSigningKey(string date, string region, string service, string secretKey)
    {
        byte[] kDate = ComputeHmac(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), Encoding.UTF8.GetBytes(date));
        byte[] kRegion = ComputeHmac(kDate, Encoding.UTF8.GetBytes(region));
        byte[] kService = ComputeHmac(kRegion, Encoding.UTF8.GetBytes(service));
        byte[] kSigning = ComputeHmac(kService, Encoding.UTF8.GetBytes("aws4_request"));
        return kSigning;
    }

    private static byte[] ComputeHmac(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static string ComputeHmacHex(byte[] key, byte[] data)
    {
        byte[] hash = ComputeHmac(key, data);
        return Convert.ToHexStringLower(hash);
    }

    private static string ComputeSha256Hex(byte[] data)
    {
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(data);
        return Convert.ToHexStringLower(hash);
    }

    #endregion
}