# Valkyrie.PackageManager（Legion）

Legion 是 Valkyrie 生态的包管理器，负责依赖管理、版本控制、安全审计和脚本执行。

## 项目结构

```
Valkyrie.PackageManager/
├── Legion.cs                     # 核心入口，三种工作模式
├── LegionManifest.cs             # legion.von 包清单
├── LegionsWorkspace.cs           # voa.workspace.v 工作区清单
├── LegionConfig.cs               # 全局/项目配置
├── LegionConfigDirectory.cs      # .config/legion/ 配置目录
├── LegionIgnore.cs               # legion.ignore 忽略规则
├── LockFile.cs                   # legion-lock.von 版本锁定
├── PackageCache.cs               # 包缓存管理
├── PackagePublisher.cs           # 包发布
├── DependencyResolver.cs         # 依赖解析与冲突检测
├── Registry.cs                   # Package 模型与 IRegistry 接口
├── Registries.cs                 # npm / jsr / conda 注册表实现
├── RegistrySourceManager.cs      # 注册表源管理与健康检查
├── ScriptRunner.cs               # 脚本执行器
├── SecurityAudit.cs              # 安全审计与许可证检查
├── SemanticVersion.cs            # 语义化版本（SemVer）
├── YearlyVersion.cs              # 年度版本号（YearlyVersion）
└── Valkyrie.PackageManager.csproj
```

## 三种工作模式

| 模式 | 检测条件 | 用途 |
|:---|:---|:---|
| **Workspace** | 存在 `voa.workspace.v` | 多包工作区 |
| **Package** | 存在 `legion.von` | 单包项目 |
| **Script** | 两者都不存在 | 直接执行命令 |

## 依赖

| 依赖 | 用途 | 来源 |
|:---|:---|:---|
| `Newtonsoft.Json` | JSON 序列化/反序列化 | NuGet |

## 详细文档

- [包管理器文档](../../documentation/core-systems/package-manager.md)
- [架构文档](../../architecture.md)
- [Legion 命令参考](../../legion.md)
- [Vendors 规范](../../vendors.md)
- [版本规范](../../version.md)
- [API 参考](../../documentation/development/api-reference.md#valkyriepackagemanager)
