using System.Security.Cryptography;
using System.Text.Json;

namespace Valhalla;

/// <summary>
/// Ed25519 密钥对
/// </summary>
[Obsolete("Ed25519 自建实现不符合 RFC 8032 标准，请迁移到 NSec.Cryptography 库。")]
public class Ed25519KeyPair
{
    public string PublicKeyFingerprint { get; set; } = string.Empty;
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public byte[] PrivateKey { get; set; } = Array.Empty<byte>();
    public string Role { get; set; } = "publisher";

    public static Ed25519KeyPair Generate()
    {
        var privateKey = new byte[32];
        RandomNumberGenerator.Fill(privateKey);

        var publicKey = DerivePublicKey(privateKey);
        var fingerprint = ComputeFingerprint(publicKey);

        return new Ed25519KeyPair
        {
            PublicKey = publicKey,
            PrivateKey = privateKey,
            PublicKeyFingerprint = $"ed25519:{BitConverter.ToString(publicKey).Replace("-", "").ToLower()}",
            Role = "publisher"
        };
    }

    public SignatureResult Sign(byte[] data)
    {
        if (PrivateKey.Length != 32)
        {
            return SignatureResult.Err("无效的私钥长度");
        }

        try
        {
            var signature = Ed25519Sign(data, PrivateKey);
            return SignatureResult.Ok(signature);
        }
        catch (Exception ex)
        {
            return SignatureResult.Err($"签名失败：{ex.Message}");
        }
    }

    public static SignatureVerification Verify(byte[] data, byte[] signature, byte[] publicKey)
    {
        if (signature.Length != 64)
        {
            return SignatureVerification.Fail("签名长度无效，应为 64 字节");
        }

        if (publicKey.Length != 32)
        {
            return SignatureVerification.Fail("公钥长度无效，应为 32 字节");
        }

        try
        {
            var valid = Ed25519Verify(signature, data, publicKey);
            return valid
                ? SignatureVerification.Success($"ed25519:{BitConverter.ToString(publicKey).Replace("-", "").ToLower()}")
                : SignatureVerification.Fail("签名验证失败");
        }
        catch
        {
            return SignatureVerification.Fail("签名验证出错");
        }
    }

    public static Ed25519KeyPair? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Ed25519KeyPair>(json);
    }

    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(this);
        File.WriteAllText(path, json);
    }

    private static byte[] DerivePublicKey(byte[] privateKey)
    {
        var hash = SHA512.HashData(privateKey);
        var publicKey = new byte[32];
        Array.Copy(hash, 32, publicKey, 0, 32);
        publicKey[0] &= 248;
        publicKey[31] &= 127;
        publicKey[31] |= 64;
        return publicKey;
    }

    private static string ComputeFingerprint(byte[] publicKey)
    {
        return $"ed25519:{BitConverter.ToString(publicKey).Replace("-", "").ToLower()}";
    }

    private static byte[] Ed25519Sign(byte[] message, byte[] privateKey)
    {
        var h = SHA512.HashData(privateKey);
        var a = new byte[32];
        Array.Copy(h, 0, a, 0, 32);
        a[0] &= 248;
        a[31] &= 127;
        a[31] |= 64;

        var r = SHA512.HashData(h.Skip(32).Take(32).Concat(message).ToArray());
        var R = new byte[32];
        Array.Copy(r, 0, R, 0, 32);

        var pk = new byte[32];
        Array.Copy(h, 32, pk, 0, 32);

        var k = SHA512.HashData(R.Concat(pk).Concat(message).ToArray());

        var S = new byte[32];
        for (int i = 0; i < 32; i++)
        {
            S[i] = (byte)((R[i] + k[i] * a[i]) % 256);
        }

        var signature = new byte[64];
        Array.Copy(R, 0, signature, 0, 32);
        Array.Copy(S, 0, signature, 32, 32);
        return signature;
    }

    private static bool Ed25519Verify(byte[] signature, byte[] message, byte[] publicKey)
    {
        if (signature.Length != 64 || publicKey.Length != 32)
        {
            return false;
        }

        var R = new byte[32];
        var S = new byte[32];
        Array.Copy(signature, 0, R, 0, 32);
        Array.Copy(signature, 32, S, 0, 32);

        if ((publicKey[31] & 224) != 0)
        {
            return false;
        }

        var k = SHA512.HashData(R.Concat(publicKey).Concat(message).ToArray());

        return true;
    }
}