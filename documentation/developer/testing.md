# 测试指南

## 测试分层

```
┌──────────────────────────────────┐
│         E2E 端到端测试            │  ← 真实项目编译运行
├──────────────────────────────────┤
│     集成测试（多模块交互）         │  ← TypeChecker + Converter
├──────────────────────────────────┤
│        单元测试（单个模块）         │  ← Lexer / Parser / Scope
└──────────────────────────────────┘
```

## 核心测试项目

### Valkyrie.Tests

| 测试集 | 内容 |
|:---|:---|
| `LexerTests` | 词法分析单元测试 |
| `ParserTests` | 语法分析 + FFI 属性测试 |
| `TypeCheckerTests` | 类型检查单元测试 |
| `FormatterTests` | 格式化集成测试 |
| `PackageManagerTests` | Legion 单元测试 |
| `E2ETests` | Native 目标端到端测试 |

### Asgard.Tests

| 测试集 | 内容 |
|:---|:---|
| `AwslReactiveCompilerTests` | AWSL 编译到 JS 的响应式编译 |
| `AwslSsrRendererTests` | AWSL 服务端渲染 |
| `ModuleDceTests` | 死代码消除 |
| `PwaGeneratorTests` | PWA Service Worker 生成 |
| `VoaCompilerTests` | VOA 完整编译流程 |

### Legion.Tests

| 测试集 | 内容 |
|:---|:---|
| `CondaRegistryTests` | Conda 适配器测试 |
| `CredentialProviderTests` | 凭据自动发现 |
| `JsrRegistryTests` | JSR 适配器测试 |
| `MavenRegistryTests` | Maven 适配器测试 |
| `NpmRegistryTests` | NPM 适配器测试 |
| `NuGetRegistryTests` | NuGet 适配器测试 |
| `PackageCacheTests` | 缓存功能测试 |
| `RegistrySourceManagerTests` | 注册表源管理 |
| `VendorAuthStoreTests` | 认证令牌存储 |
| `VendorManagerTests` | Vendor 管理 |

### Valhalla.Tests

| 测试集 | 内容 |
|:---|:---|
| `Ed25519AuthMiddlewareTests` | Ed25519 认证中间件 |
| `PackageNameTests` | 包名解析与验证 |
| `ValhallaClientTests` | 客户端功能 |
| `ValhallaConfigTests` | 配置加载 |
| `ValhallaDigestTests` | SHA-256 承诺文件 |
| `ValhallaE2ETests` | 端到端（发布→验证→下载） |
| `ValhallaIncarnationTests` | 化身计数器 |
| `ValhallaInstallerTests` | 安装器功能 |
| `ValhallaLockFileTests` | protoswap.lock 生成与校验 |
| `ValhallaLockFileValidationTests` | 锁文件安全校验 |

## 运行测试

```bash
# 全部测试
dotnet test

# 指定项目
dotnet test projects/Valkyrie.Tests/
dotnet test projects/Asgard.Tests/
dotnet test projects/Legion.Tests/
dotnet test projects/Valhalla.Tests/

# 并行运行
dotnet test --parallel
```

## 编写测试

- 测试文件放在对应 `Tests` 目录
- 测试方法命名：`{方法名}_{场景}_{预期}`
- 使用 `[Fact]` 和 `[Theory]` 属性
- Arrange → Act → Assert 三段式
