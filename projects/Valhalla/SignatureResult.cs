using System;
using System.IO;
using System.Linq;

namespace Valhalla;

/// <summary>
/// Ed25519 签名结果
/// </summary>
/// <remarks>
/// ⚠️ Obsolete: 此文件中的自建 Ed25519 实现（Ed25519Sign/Ed25519Verify）是朴素的逐字节模拟，
/// 未在 GF(2^255-19) 有限域中正确计算，且 Verify 总是返回 true，不符合 RFC 8032 标准。
/// 请迁移到 Valhalla.Server 中使用的 NSec.Cryptography 库进行 Ed25519 签名验证。
/// 迁移路径：使用 NSec.Cryptography.Algorithm.Ed25519 的 Sign 和 Verify 方法。
/// </remarks>
[Obsolete("Ed25519 自建实现不符合 RFC 8032 标准，请迁移到 NSec.Cryptography 库。Verification 端（Valhalla.Server/Ed25519AuthMiddleware.cs）已使用 NSec 正确实现。")]
public class SignatureResult
{
    public bool Success { get; set; }
    public byte[] SignatureBytes { get; set; } = Array.Empty<byte>();
    public string? Error { get; set; }

    public static SignatureResult Ok(byte[] signature) =>
        new() { Success = true, SignatureBytes = signature };

    public static SignatureResult Err(string error) =>
        new() { Success = false, Error = error };
}