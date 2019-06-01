using System.Net;

namespace Valkyrie.Tests.PackageManagerTests;

public class PackagePublisherTests
{
    [Fact]
    public void ValidatePackage_ShouldReturnTrueForValidOptions()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);

            var handler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(handler);
            var registries = new Dictionary<string, IRegistry> { { "npm", new NpmRegistry(httpClient) } };
            var publisher = new PackagePublisher(registries);

            var options = new PublishOptions
            {
                PackageName = "test-pkg",
                Version = "1.0.0",
                PackagePath = tempDir
            };

            Assert.True(publisher.ValidatePackage(options));
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
    public void ValidatePackage_ShouldReturnFalseForInvalidVersion()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);

            var handler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(handler);
            var registries = new Dictionary<string, IRegistry> { { "npm", new NpmRegistry(httpClient) } };
            var publisher = new PackagePublisher(registries);

            var options = new PublishOptions
            {
                PackageName = "test-pkg",
                Version = "not-a-version",
                PackagePath = tempDir
            };

            Assert.False(publisher.ValidatePackage(options));
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
    public void CreateTarball_ShouldCreateNonEmptyTarball()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "index.js"), "console.log('hello')");
            File.WriteAllText(Path.Combine(tempDir, "package.json"), "{\"name\":\"test\"}");

            var handler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(handler);
            var registries = new Dictionary<string, IRegistry> { { "npm", new NpmRegistry(httpClient) } };
            var publisher = new PackagePublisher(registries);

            byte[] tarball = publisher.CreateTarball(tempDir, "test-pkg", "1.0.0");

            Assert.NotEmpty(tarball);
            Assert.True(tarball.Length > 0);
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
    public void ComputeIntegrity_ShouldReturnSha512Hash()
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes("test data");
        string integrity = PackagePublisher.ComputeIntegrity(data);

        Assert.StartsWith("sha512-", integrity);
        Assert.True(integrity.Length > "sha512-".Length);
    }

    [Fact]
    public async Task PublishAsync_ShouldCreateTarballAndCallRegistry()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"legion-test-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "index.js"), "console.log('hello')");

            var handler = new MockHttpMessageHandler();
            handler.RegisterHandler("https://registry.npmjs.org/test-pkg", _ =>
                new HttpResponseMessage(HttpStatusCode.OK));

            var httpClient = new HttpClient(handler);
            var registries = new Dictionary<string, IRegistry> { { "npm", new NpmRegistry(httpClient) } };
            var publisher = new PackagePublisher(registries);

            var result = await publisher.PublishAsync(new PublishOptions
            {
                PackageName = "test-pkg",
                Version = "1.0.0",
                PackagePath = tempDir,
                RegistryName = "npm",
                AuthToken = "test-token"
            });

            Assert.True(result.Success);
            Assert.Equal("test-pkg", result.PackageName);
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