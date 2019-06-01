namespace Legion.Security;

public class SignatureVerificationResult
{
    public string PackageName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? Signer { get; set; }
    public string? Error { get; set; }
}