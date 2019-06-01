namespace Valkyrie.Tests.PackageManagerTests;

public class SecurityAuditTests
{
    [Fact]
    public async Task AuditPackageAsync_ShouldReturnResult()
    {
        var audit = new SecurityAudit();
        var package = new Package { Name = "test-pkg", Version = "1.0.0", License = "MIT", Dependencies = new List<string>() };

        var result = await audit.AuditPackageAsync(package);

        Assert.NotNull(result);
        Assert.NotNull(result.Vulnerabilities);
        Assert.NotNull(result.Licenses);
    }

    [Fact]
    public void IsLicenseCompatible_ShouldRecognizeCompatibleLicenses()
    {
        var audit = new SecurityAudit();

        Assert.True(audit.IsLicenseCompatible("MIT"));
        Assert.True(audit.IsLicenseCompatible("Apache-2.0"));
        Assert.True(audit.IsLicenseCompatible("BSD-3-Clause"));
    }

    [Fact]
    public void IsLicenseCompatible_ShouldRejectIncompatibleLicenses()
    {
        var audit = new SecurityAudit();

        Assert.False(audit.IsLicenseCompatible("GPL-3.0"));
        Assert.False(audit.IsLicenseCompatible("AGPL-3.0"));
    }

    [Fact]
    public void VerifyIntegrity_ShouldComputeRealHash()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            string testFile = Path.Combine(tempDir, "test.txt");
            File.WriteAllText(testFile, "hello world");

            var audit = new SecurityAudit();
            string hash = LockFile.ComputeFileIntegrity(testFile);

            Assert.True(audit.VerifyIntegrity(testFile, hash));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void VerifyIntegrity_ShouldReturnFalseForWrongHash()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            string testFile = Path.Combine(tempDir, "test.txt");
            File.WriteAllText(testFile, "hello world");

            var audit = new SecurityAudit();

            Assert.False(audit.VerifyIntegrity(testFile, "sha512-wronghash"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}