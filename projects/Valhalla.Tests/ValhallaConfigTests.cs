using Oak.Data;

using Xunit;
using Valhalla.Config;

namespace Valhalla.Tests;

public class ValhallaConfigTests
{
    [Fact]
    public void CreateDefault_产生有效默认配置()
    {
        var config = ValhallaConfig.CreateDefault();
        Assert.Equal("瓦尓哈拉", config.Name);
        Assert.Equal(8080, config.Port);
        Assert.Equal(RegistrationMode.Open, config.Registration);
        Assert.Equal(StorageBackend.Local, config.Storage);
        Assert.True(config.Public);
    }

    [Fact]
    public void Validate_默认配置_有效()
    {
        var config = ValhallaConfig.CreateDefault();
        var errors = config.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_名称为空_报错()
    {
        var config = ValhallaConfig.CreateDefault();
        config.Name = "  ";
        var errors = config.Validate();
        Assert.Contains("实例名称不能为空", errors);
    }

    [Fact]
    public void Validate_端口越界_报错()
    {
        var config = ValhallaConfig.CreateDefault();
        config.Port = 0;
        var errors = config.Validate();
        Assert.Contains("端口号必须在 1-65535 范围内", errors);
    }

    [Fact]
    public void Validate_S3缺少配置_报错()
    {
        var config = ValhallaConfig.CreateDefault();
        config.Storage = StorageBackend.S3;
        var errors = config.Validate();
        Assert.Contains("使用 S3 存储时必须配置 S3 信息", errors);
    }

    [Fact]
    public void 往返序列化_默认配置_一致()
    {
        var original = ValhallaConfig.CreateDefault();
        original.Name = "测试实例";
        original.Description = "一个测试";
        original.CoolingPeriodDays = 7;

        string von = original.ToVonString();
        var parsed = ValhallaConfig.Parse(von);

        Assert.Equal(original.Name, parsed.Name);
        Assert.Equal(original.Description, parsed.Description);
        Assert.Equal(original.CoolingPeriodDays, parsed.CoolingPeriodDays);
        Assert.Equal(original.Port, parsed.Port);
        Assert.Equal(original.Registration, parsed.Registration);
    }

    [Fact]
    public void 往返序列化_S3配置_一致()
    {
        var original = new ValhallaConfig
        {
            Storage = StorageBackend.S3,
            S3 = new S3Config
            {
                Endpoint = "https://s3.example.com",
                Bucket = "my-bucket",
                Region = "us-west-2"
            }
        };

        string von = original.ToVonString();
        var parsed = ValhallaConfig.Parse(von);

        Assert.Equal(StorageBackend.S3, parsed.Storage);
        Assert.NotNull(parsed.S3);
        Assert.Equal("https://s3.example.com", parsed.S3!.Endpoint);
        Assert.Equal("my-bucket", parsed.S3.Bucket);
    }

    [Fact]
    public void 解析_邀请码模式_正确()
    {
        string von = @"{ 
    registration: ""invite"",
    inviteCode: ""let-me-in""
}";
        var config = ValhallaConfig.Parse(von);
        Assert.Equal(RegistrationMode.Invite, config.Registration);
        Assert.Equal("let-me-in", config.InviteCode);
    }

    [Fact]
    public void 解析_关闭注册_正确()
    {
        string von = @"{ registration: ""closed"" }";
        var config = ValhallaConfig.Parse(von);
        Assert.Equal(RegistrationMode.Closed, config.Registration);
    }

    [Fact]
    public void 解析_非对象_抛出异常()
    {
        Assert.Throws<ArgumentException>(() => ValhallaConfig.Parse("\"hello\""));
    }

    [Fact]
    public async Task SaveAsync_写入文件_可重新加载()
    {
        var config = ValhallaConfig.CreateDefault();
        config.Name = "写入测试";

        string tempFile = Path.Combine(Path.GetTempPath(),
            $"valhalla-test-{Guid.NewGuid():N}.von");

        try
        {
            await config.SaveAsync(tempFile);
            Assert.True(File.Exists(tempFile));

            var loaded = await ValhallaConfig.LoadAsync(tempFile);
            Assert.Equal(config.Name, loaded.Name);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}