using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oak.Data;
using Oak.Von;

namespace Valhalla.Config;

/// <summary>
/// 瓦尓哈拉服务端配置模型
/// </summary>
public class ValhallaConfig
{
    #region 基础信息

    /// <summary>实例名称</summary>
    public string Name { get; set; } = "瓦尓哈拉";

    /// <summary>实例描述</summary>
    public string Description { get; set; } = string.Empty;

    #endregion

    #region 可见性与注册

    /// <summary>是否为公开实例</summary>
    public bool Public { get; set; } = true;

    /// <summary>注册模式</summary>
    public RegistrationMode Registration { get; set; } = RegistrationMode.Open;

    /// <summary>邀请码（当注册模式为 Invite 时使用）</summary>
    public string? InviteCode { get; set; }

    #endregion

    #region 安全策略

    /// <summary>PURGE 后的冷却期天数</summary>
    public int CoolingPeriodDays { get; set; } = 14;

    /// <summary>是否要求管理员审批重注册</summary>
    public bool RequireAdminForReRegistration { get; set; } = true;

    /// <summary>是否强制发布者提供 Ed25519 公钥</summary>
    public bool PubkeyRequired { get; set; } = true;

    #endregion

    #region 存储配置

    /// <summary>存储后端类型</summary>
    public StorageBackend Storage { get; set; } = StorageBackend.Local;

    /// <summary>存储路径（Local 模式下的目录路径，S3 模式下的桶名）</summary>
    public string StoragePath { get; set; } = "./data";

    /// <summary>S3 配置（仅当 Storage 为 S3 时使用）</summary>
    public S3Config? S3 { get; set; }

    #endregion

    #region 限制

    /// <summary>单个包最大大小（MB）</summary>
    public int MaxPackageSizeMB { get; set; } = 100;

    /// <summary>最大总存储空间（GB）</summary>
    public int MaxTotalStorageGB { get; set; } = 50;

    #endregion

    #region 功能开关

    /// <summary>审计日志是否公开可访问</summary>
    public bool EnableAuditPublicAccess { get; set; } = true;

    /// <summary>是否启用 Prometheus 指标</summary>
    public bool EnableMetrics { get; set; } = true;

    #endregion

    #region 网络配置

    /// <summary>监听端口</summary>
    public int Port { get; set; } = 8080;

    /// <summary>CORS 配置</summary>
    public CorsConfig Cors { get; set; } = new();

    #endregion

    #region 序列化

    /// <summary>
    /// 从 valhalla.von 文件加载配置
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    public static async Task<ValhallaConfig> LoadAsync(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"配置文件不存在: {configPath}", configPath);
        }

        string content = await File.ReadAllTextAsync(configPath);
        return Parse(content);
    }

    /// <summary>
    /// 从 von 格式文本解析配置
    /// </summary>
    /// <param name="content">von 格式内容</param>
    public static ValhallaConfig Parse(string content)
    {
        var parser = new GonParser();
        var value = parser.Deserialize(content);
        if (value.Fields is null)
        {
            throw new ArgumentException("配置文件必须是对象格式");
        }

        return FromSerde(value);
    }

    /// <summary>
    /// 保存配置到 valhalla.von 文件
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    public async Task SaveAsync(string configPath)
    {
        string? dir = Path.GetDirectoryName(configPath);
        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var serde = ToSerde();
        string content = FormatVon(serde);
        await File.WriteAllTextAsync(configPath, content);
    }

    /// <summary>
    /// 将配置序列化为 von 格式字符串
    /// </summary>
    public string ToVonString()
    {
        return FormatVon(ToSerde());
    }

    #endregion

    #region Serde 转换

    private static ValhallaConfig FromSerde(SerdeValue obj)
    {
        var config = new ValhallaConfig();

        config.Name = GetFieldString(obj, "name") ?? "瓦尓哈拉";
        config.Description = GetFieldString(obj, "description") ?? string.Empty;
        config.Public = obj.GetField("public")?.GetBoolean() ?? true;

        string? regMode = GetFieldString(obj, "registration");
        config.Registration = regMode switch
        {
            "invite" => RegistrationMode.Invite,
            "closed" => RegistrationMode.Closed,
            _ => RegistrationMode.Open
        };

        config.InviteCode = GetFieldString(obj, "inviteCode");
        config.CoolingPeriodDays = ParseInt(obj.GetField("coolingPeriodDays"), 14);
        config.RequireAdminForReRegistration = obj.GetField("requireAdminForReRegistration")?.GetBoolean() ?? true;
        config.PubkeyRequired = obj.GetField("pubkeyRequired")?.GetBoolean() ?? true;

        string? storageMode = GetFieldString(obj, "storage");
        config.Storage = storageMode switch
        {
            "s3" => StorageBackend.S3,
            _ => StorageBackend.Local
        };

        config.StoragePath = GetFieldString(obj, "storagePath") ?? "./data";

        var s3Field = obj.GetField("s3");
        if (s3Field is not null && s3Field.Fields is not null)
        {
            config.S3 = new S3Config
            {
                Endpoint = GetFieldString(s3Field, "endpoint") ?? string.Empty,
                Bucket = GetFieldString(s3Field, "bucket") ?? string.Empty,
                AccessKey = GetFieldString(s3Field, "accessKey") ?? string.Empty,
                SecretKey = GetFieldString(s3Field, "secretKey") ?? string.Empty,
                Region = GetFieldString(s3Field, "region") ?? "us-east-1"
            };
        }

        config.MaxPackageSizeMB = ParseInt(obj.GetField("maxPackageSizeMB"), 100);
        config.MaxTotalStorageGB = ParseInt(obj.GetField("maxTotalStorageGB"), 50);
        config.EnableAuditPublicAccess = obj.GetField("enableAuditPublicAccess")?.GetBoolean() ?? true;
        config.EnableMetrics = obj.GetField("enableMetrics")?.GetBoolean() ?? true;
        config.Port = ParseInt(obj.GetField("port"), 8080);

        var corsField = obj.GetField("cors");
        if (corsField is not null && corsField.Fields is not null)
        {
            var originsField = corsField.GetField("origins");
            config.Cors.Origins = ParseStringArray(originsField);
        }

        return config;
    }

    private SerdeValue ToSerde()
    {
        var fields = new Dictionary<string, SerdeValue>
        {
            ["name"] = SerdeValue.String(Name),
            ["description"] = SerdeValue.String(Description),
            ["public"] = SerdeValue.Boolean(Public),
            ["registration"] = SerdeValue.String(Registration switch
            {
                RegistrationMode.Invite => "invite",
                RegistrationMode.Closed => "closed",
                _ => "open"
            }),
            ["coolingPeriodDays"] = SerdeValue.Integer(CoolingPeriodDays.ToString()),
            ["requireAdminForReRegistration"] = SerdeValue.Boolean(RequireAdminForReRegistration),
            ["pubkeyRequired"] = SerdeValue.Boolean(PubkeyRequired),
            ["storage"] = SerdeValue.String(Storage switch
            {
                StorageBackend.S3 => "s3",
                _ => "local"
            }),
            ["storagePath"] = SerdeValue.String(StoragePath),
            ["maxPackageSizeMB"] = SerdeValue.Integer(MaxPackageSizeMB.ToString()),
            ["maxTotalStorageGB"] = SerdeValue.Integer(MaxTotalStorageGB.ToString()),
            ["enableAuditPublicAccess"] = SerdeValue.Boolean(EnableAuditPublicAccess),
            ["enableMetrics"] = SerdeValue.Boolean(EnableMetrics),
            ["port"] = SerdeValue.Integer(Port.ToString())
        };

        if (!string.IsNullOrWhiteSpace(InviteCode))
        {
            fields["inviteCode"] = SerdeValue.String(InviteCode);
        }

        if (S3 is not null)
        {
            var s3Fields = new Dictionary<string, SerdeValue>
            {
                ["endpoint"] = SerdeValue.String(S3.Endpoint),
                ["bucket"] = SerdeValue.String(S3.Bucket),
                ["accessKey"] = SerdeValue.String(S3.AccessKey),
                ["secretKey"] = SerdeValue.String(S3.SecretKey),
                ["region"] = SerdeValue.String(S3.Region)
            };
            fields["s3"] = SerdeValue.Object(s3Fields);
        }

        var corsFields = new Dictionary<string, SerdeValue>
        {
            ["origins"] = SerdeValue.Array(
                Cors.Origins.Select(o => SerdeValue.String(o)).ToList())
        };
        fields["cors"] = SerdeValue.Object(corsFields);

        return SerdeValue.Object(fields);
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static ValhallaConfig CreateDefault()
    {
        return new ValhallaConfig();
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    /// <returns>验证错误列表，空列表表示有效</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("实例名称不能为空");
        }

        if (MaxPackageSizeMB <= 0)
        {
            errors.Add("单个包最大大小必须大于 0");
        }

        if (MaxTotalStorageGB <= 0)
        {
            errors.Add("最大总存储空间必须大于 0");
        }

        if (CoolingPeriodDays < 0)
        {
            errors.Add("冷却期天数不能为负数");
        }

        if (Port <= 0 || Port > 65535)
        {
            errors.Add("端口号必须在 1-65535 范围内");
        }

        if (Storage == StorageBackend.S3 && S3 is null)
        {
            errors.Add("使用 S3 存储时必须配置 S3 信息");
        }

        if (Storage == StorageBackend.S3 && S3 is not null)
        {
            if (string.IsNullOrWhiteSpace(S3.Endpoint))
            {
                errors.Add("S3 端点地址不能为空");
            }

            if (string.IsNullOrWhiteSpace(S3.Bucket))
            {
                errors.Add("S3 存储桶名称不能为空");
            }
        }

        return errors;
    }

    #endregion

    #region 私有辅助

    private static string? GetFieldString(SerdeValue obj, string fieldName)
    {
        return obj.GetField(fieldName)?.GetString();
    }

    private static int ParseInt(SerdeValue? value, int defaultValue)
    {
        if (value is null)
        {
            return defaultValue;
        }

        string? intStr = value.GetIntegerString();
        if (intStr is not null && int.TryParse(intStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        return defaultValue;
    }

    private static List<string> ParseStringArray(SerdeValue? value)
    {
        if (value?.Elements is null)
        {
            return new List<string>();
        }

        return value.Elements
            .Select(e => e.GetString())
            .Where(s => s is not null)
            .Cast<string>()
            .ToList();
    }

    private static string FormatVon(SerdeValue value, int indent = 0)
    {
        var sb = new StringBuilder();
        FormatValue(sb, value, indent);
        return sb.ToString();
    }

    private static void FormatValue(StringBuilder sb, SerdeValue value, int indent)
    {
        switch (value.Type)
        {
            case SerdeValueType.Null:
                sb.Append("null");
                break;
            case SerdeValueType.Boolean:
                sb.Append(value.GetBoolean() ? "true" : "false");
                break;
            case SerdeValueType.Integer:
                sb.Append(value.GetIntegerString() ?? "0");
                break;
            case SerdeValueType.Decimal:
                sb.Append(value.GetDecimalString() ?? "0.0");
                break;
            case SerdeValueType.String:
                sb.Append('"');
                sb.Append(EscapeString(value.GetString() ?? ""));
                sb.Append('"');
                break;
            case SerdeValueType.Array:
                FormatVonArray(sb, value, indent);
                break;
            case SerdeValueType.Object:
                FormatVonObject(sb, value, indent);
                break;
        }
    }

    private static void FormatVonObject(StringBuilder sb, SerdeValue value, int indent)
    {
        if (value.Fields is null || value.Fields.Count == 0)
        {
            sb.Append("{}");
            return;
        }

        string indentStr = new string(' ', indent * 4);
        string innerIndent = new string(' ', (indent + 1) * 4);

        sb.AppendLine("{");

        var fieldList = value.Fields.ToList();
        for (int i = 0; i < fieldList.Count; i++)
        {
            var (key, fieldValue) = fieldList[i];
            sb.Append(innerIndent);
            sb.Append(key);
            sb.Append(": ");

            if (fieldValue.Type is SerdeValueType.Object or SerdeValueType.Array)
            {
                FormatValue(sb, fieldValue, indent + 1);
            }
            else
            {
                FormatValue(sb, fieldValue, 0);
            }

            if (i < fieldList.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.Append(indentStr);
        sb.Append('}');
    }

    private static void FormatVonArray(StringBuilder sb, SerdeValue value, int indent)
    {
        if (value.Elements is null || value.Elements.Count == 0)
        {
            sb.Append("[]");
            return;
        }

        bool allSimple = value.Elements.All(e =>
            e.Type is not SerdeValueType.Object and not SerdeValueType.Array);

        if (allSimple)
        {
            sb.Append("[ ");
            for (int i = 0; i < value.Elements.Count; i++)
            {
                FormatValue(sb, value.Elements[i], 0);
                if (i < value.Elements.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(" ]");
        }
        else
        {
            string indentStr = new string(' ', indent * 4);
            string innerIndent = new string(' ', (indent + 1) * 4);

            sb.AppendLine("[");
            for (int i = 0; i < value.Elements.Count; i++)
            {
                sb.Append(innerIndent);
                FormatValue(sb, value.Elements[i], indent + 1);
                if (i < value.Elements.Count - 1)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    sb.AppendLine();
                }
            }
            sb.Append(indentStr);
            sb.Append(']');
        }
    }

    private static string EscapeString(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    #endregion
}