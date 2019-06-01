namespace Valhalla;

/// <summary>
/// Ed25519 签名及验证结果
/// </summary>
[Obsolete("Ed25519 自建实现不符合 RFC 8032 标准，请迁移到 NSec.Cryptography 库。")]
public class SignatureVerification
{
    public bool Valid { get; set; }
    public string? PublicKeyFingerprint { get; set; }
    public string? Error { get; set; }

    public static SignatureVerification Success(string fingerprint) =>
        new() { Valid = true, PublicKeyFingerprint = fingerprint };

    public static SignatureVerification Fail(string error) =>
        new() { Valid = false, Error = error };
}