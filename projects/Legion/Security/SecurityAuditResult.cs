namespace Legion.Security;

public class SecurityAuditResult
{
    public List<VulnerabilityReport> Vulnerabilities { get; set; } = new();
    public List<LicenseInfo> Licenses { get; set; } = new();
    public List<SignatureVerificationResult> Signatures { get; set; } = new();
    public bool HasVulnerabilities => Vulnerabilities.Count > 0;
    public bool HasLicenseIssues => Licenses.Exists(l => !l.IsCompatible);
    public bool HasSignatureIssues => Signatures.Exists(s => !s.IsValid);
}