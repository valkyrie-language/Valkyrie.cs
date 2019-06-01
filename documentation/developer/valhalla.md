# 瓦尓哈拉 (Valhalla) 注册表

瓦尓哈拉是 Nyar 组织的专用二进制注册表，承载 `.nyar` 字节码与源码分发包。不同于 npm/jsr 等传统注册表，瓦尓哈拉是一个**可自托管、去中心化可部署**的信任根注册表。

> 瓦尓哈拉，为永恒而存储。

## 核心特性

| 特性 | 说明 |
|:---|:---|
| **二进制写后不可变** | SHA-256 承诺文件与二进制分离存储，包内容不可修改 |
| **化身计数器** | PURGE 后递增，阻止恶意重注册 |
| **Ed25519 发布者身份** | 每个包绑定发布者公钥指纹 |
| **组织命名空间与授权链** | `org.pkg` 形式的包名由根组织逐级授权 |
| **`protoswap.lock` 信任锚点** | 记录 incarnation、发布者、SHA-256 完整校验链 |
| **审计日志** | 所有管理操作记录在 JSONL 审计日志中 |

## API 端点

| 端点 | 方法 | 说明 |
|:---|:---|:---|
| `/api/packages/{name}/manifest` | GET | 获取版本清单和元数据 |
| `/api/packages?query=` | GET | 搜索包 |
| `/api/packages/{name}/versions/{version}/download` | GET | 下载 `.nyar` + 源码 |
| `/api/packages/{name}/versions` | PUT | 发布新版本（需 Ed25519 签名） |

## 认证

瓦尓哈拉使用 **Ed25519 密钥对签名**认证，非传统的令牌制。发布者生成 Ed25519 密钥对，用私钥签发发布请求，公钥指纹绑定到包。

## Legion 集成

通过 `Legion.Registry.Valhalla` 程序集中的 `ValhallaRegistry` 实现适配：

```csharp
using Valkyrie.PackageManager;

var legion = new Legion();
legion.RegisterRegistry(new ValhallaRegistry("https://valhalla.example.com"));
```

## 自托管部署

瓦尓哈拉设计为可完全自托管。部署私有实例时需配置：

- 存储后端：本地文件系统 / S3 兼容存储
- Ed25519 密钥对
- 组织命名空间配置
- TLS 证书

## 项目结构

```
Valhalla/
├── Valhalla/                  # 共享核心库（数据模型、签名、审计）
├── Valhalla.Client/           # 客户端库（下载、安装、锁文件校验）
├── Valhalla.Config/           # 配置模型
├── Valhalla.Server/           # HTTP 服务端（ASP.NET Core）
│   ├── Auth/                  # Ed25519 认证中间件
│   └── Storage/               # 存储抽象（Local / S3）
└── Legion.Registry.Valhalla/  # Legion IRegistry 适配器
```

## 安全模型

### 威胁模型

瓦尓哈拉抵御以下攻击向量：

| 威胁 | 防御 |
|:---|:---|
| 包内容篡改 | SHA-256 承诺文件，二进制不可变 |
| 恶意重注册 | 化身计数器递增 |
| 身份冒充 | Ed25519 公钥指纹绑定 |
| 命名空间抢夺 | 组织逐级授权链 |

### 二进制不可变原则

一旦版本发布，其二进制内容永久固定。即使执行 PURGE 操作，化身计数器递增使得旧名称+旧化身永远无法再被写入，确保锁文件的信任锚点不被破坏。
