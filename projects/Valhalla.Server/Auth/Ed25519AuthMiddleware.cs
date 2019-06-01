using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Valhalla;

namespace Valhalla.Server.Auth;

/// <summary>
/// Ed25519 请求认证中间件，验证 API 写操作的签名
/// </summary>
public class Ed25519AuthMiddleware
{
    /// <summary>Ed25519 公钥原始长度（字节）</summary>
    private const int Ed25519PublicKeyLength = 32;

    /// <summary>Ed25519 签名长度（字节）</summary>
    private const int Ed25519SignatureLength = 64;

    private readonly RequestDelegate _next;
    private readonly Dictionary<string, byte[]> _trustedKeys;

    /// <summary>
    /// 非认证白名单路径前缀
    /// </summary>
    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/packages",
        "/api/orgs"
    };

    /// <summary>
    /// 创建认证中间件
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="trustedKeys">可信公钥字典，键为公钥指纹，值为公钥字节</param>
    public Ed25519AuthMiddleware(RequestDelegate next, Dictionary<string, byte[]> trustedKeys)
    {
        _next = next;
        _trustedKeys = trustedKeys;
    }

    /// <summary>
    /// 中间件入口
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
        {
            await _next(context);
            return;
        }

        string authHeader = GetAuthHeader(context);

        if (string.IsNullOrEmpty(authHeader))
        {
            context.Response.StatusCode = 401;
            await WriteJsonError(context, "缺少 Authorization 头");
            return;
        }

        context.Request.EnableBuffering();

        byte[] bodyHash;
        try
        {
            using var memoryStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(memoryStream);
            bodyHash = SHA256.HashData(memoryStream.ToArray());
            context.Request.Body.Position = 0;
        }
        catch
        {
            context.Response.StatusCode = 400;
            await WriteJsonError(context, "无法读取请求体");
            return;
        }

        var authResult = ValidateAuthHeader(authHeader, bodyHash);
        if (!authResult.Valid)
        {
            context.Response.StatusCode = 401;
            await WriteJsonError(context, authResult.Error ?? "认证失败");
            return;
        }

        context.Items["ValhallaActorFingerprint"] = authResult.PublicKeyFingerprint ?? "unknown";

        await _next(context);
    }

    /// <summary>
    /// 获取 Authorization 请求头值
    /// </summary>
    private static string GetAuthHeader(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out StringValues values))
        {
            string? header = values.FirstOrDefault();
            if (header is not null && header.StartsWith("ed25519-signature(", StringComparison.OrdinalIgnoreCase))
            {
                return header;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// 验证 Ed25519 认证头
    /// </summary>
    /// <param name="authHeader">Authorization 头值</param>
    /// <param name="bodyHash">请求体 SHA-256 哈希</param>
    private SignatureVerification ValidateAuthHeader(string authHeader, byte[] bodyHash)
    {
        try
        {
            int start = authHeader.IndexOf('(');
            int end = authHeader.LastIndexOf(')');
            if (start < 0 || end < 0 || end <= start)
            {
                return SignatureVerification.Fail("认证头格式无效");
            }

            string args = authHeader.Substring(start + 1, end - start - 1);
            string[] parts = args.Split(',');
            if (parts.Length < 2)
            {
                return SignatureVerification.Fail("认证头参数不足");
            }

            string fingerprint = parts[0].Trim();
            string signatureBase64 = parts[1].Trim();

            if (!_trustedKeys.TryGetValue(fingerprint, out byte[]? publicKey))
            {
                return SignatureVerification.Fail($"未知的公钥指纹: {fingerprint}");
            }

            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromBase64String(signatureBase64);
            }
            catch
            {
                return SignatureVerification.Fail("签名 Base64 解码失败");
            }

            bool verified = VerifyEd25519(publicKey, bodyHash, signatureBytes);

            return verified
                ? SignatureVerification.Success(fingerprint)
                : SignatureVerification.Fail("Ed25519 签名验证失败");
        }
        catch (Exception ex)
        {
            return SignatureVerification.Fail($"认证处理异常: {ex.Message}");
        }
    }

    /// <summary>
    /// Ed25519 签名验证
    /// </summary>
    /// <param name="publicKey">32 字节 Ed25519 公钥</param>
    /// <param name="data">签名载荷数据</param>
    /// <param name="signature">64 字节 Ed25519 签名</param>
    /// <returns>签名有效返回 true</returns>
    private static bool VerifyEd25519(byte[] publicKey, byte[] data, byte[] signature)
    {
        if (publicKey.Length != Ed25519PublicKeyLength || signature.Length != Ed25519SignatureLength)
        {
            return false;
        }

        try
        {
            var algorithm = NSec.Cryptography.SignatureAlgorithm.Ed25519;
            var publicKeyObj = NSec.Cryptography.PublicKey.Import(
                algorithm, publicKey, NSec.Cryptography.KeyBlobFormat.RawPublicKey);
            return algorithm.Verify(publicKeyObj, data, signature);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 写入 JSON 错误响应
    /// </summary>
    private static async Task WriteJsonError(HttpContext context, string message)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        string json = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(json);
    }

    /// <summary>注册为简便的中间件扩展</summary>
    public static IApplicationBuilder UseEd25519Auth(
        IApplicationBuilder builder, Dictionary<string, byte[]> trustedKeys)
    {
        return builder.UseMiddleware<Ed25519AuthMiddleware>(trustedKeys);
    }
}