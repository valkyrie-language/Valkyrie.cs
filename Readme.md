# Valkyrie

Valkyrie 是面向游戏引擎生态的领域特定语言（DSL）及其完整工具链，为 Gnosis 元游戏引擎提供编译、运行和包管理能力。

> 女武神，为战场而生。

## 核心理念

- **编译器与包管理器严格分离**：VCC 不知道 Legion 的存在，Legion 可以调用 VCC 执行脚本
- **多注册表统一管理**：同时支持 npm、JSR、Conda 三大注册表，统一 vendors 路径规范
- **年度版本号（YearlyVersion）**：用 `yearly.major.minor.patch` 替代传统 SemVer，消除预发布标记歧义
- **ECS 原生语言设计**：`component`、`system`、`widget`、`plugin` 一等公民语法

## 项目结构

```
Valkyrie.cs/
├── Valkyrie.sln                              # 解决方案文件
├── Readme.md                                 # 项目总览（本文件）
├── documentation/                            # 完整文档体系
│   ├── overview/                             # 概述文档
│   │   ├── index.md                          # 文档首页
│   │   ├── introduction.md                   # 项目介绍
│   │   ├── design-philosophy.md              # 设计哲学
│   │   └── quick-start.md                    # 快速开始
│   ├── core-systems/                         # 核心系统文档
│   │   ├── vcc.md                            # VCC 编译器
│   │   ├── runtime.md                        # 运行时
│   │   ├── type-checker.md                   # 类型检查器
│   │   └── package-manager.md                # 包管理器
│   ├── language/                             # 语言参考
│   │   ├── index.md                          # 语言参考首页
│   │   ├── lexical-structure.md              # 词法结构
│   │   ├── types.md                          # 类型系统
│   │   ├── declarations.md                   # 声明
│   │   ├── statements.md                     # 语句
│   │   ├── expressions.md                    # 表达式
│   │   ├── attributes.md                     # 特性标注
│   │   ├── ecs-extension.md                  # ECS 扩展
│   │   ├── schema-extension.md               # Schema 扩展（衍生：Hermes）
│   │   ├── template-extension.md             # Template 扩展（衍生：DejavuEngine）
│   │   ├── shader-extension.md               # Shader 扩展
│   │   └── neural-extension.md               # Neural 扩展
│   ├── toolchain/                            # 工具链
│   │   ├── index.md                          # 工具链首页
│   │   ├── vcc-reference.md                  # VCC 命令参考
│   │   ├── legion-reference.md               # Legion 命令参考
│   │   ├── formatter.md                      # 代码格式化器
│   │   ├── linter.md                         # 代码检查器
│   │   ├── version.md                        # 版本规范
│   │   └── vendors.md                        # Vendors 规范
│   ├── development/                          # 开发文档
│   │   ├── getting-started.md                # 开发指南
│   │   └── api-reference.md                  # API 参考
│   └── maintenance/                          # 维护者文档
│       ├── index.md                          # 维护者首页
│       ├── architecture.md                   # 架构详解
│       ├── coding-conventions.md             # 编码规范
│       ├── testing.md                        # 测试指南
│       └── dependency-rules.md               # 依赖规则
├── projects/
│   ├── Valkyrie/                             # VCC 编译器入口
│   │   ├── Program.cs                        # 命令行入口（REPL, install, resolve, compile）
│   │   ├── Readme.md                         # VCC 使用文档
│   │   └── Valkyrie.csproj
│   ├── Valkyrie.Runtime/                     # 运行时
│   │   ├── ValkyrieRuntime.cs                # 编译管线编排
│   │   ├── Converter/AstToIkunConverter.cs   # AST→IKun 转换器
│   │   └── Valkyrie.Runtime.csproj
│   ├── Valkyrie.TypeChecker/                 # 类型检查器
│   │   ├── TypeChecker.cs                    # 类型检查核心
│   │   ├── TypeDiagnostic.cs                 # 诊断输出
│   │   ├── Scope/                            # 作用域管理
│   │   ├── TypeSystem/                       # 类型系统
│   │   └── Valkyrie.TypeChecker.csproj
│   ├── Valkyrie.Formatter/                   # 代码格式化器
│   │   ├── CodeFormatter.cs                  # 格式化核心
│   │   ├── FormatterConfig.cs                # 格式化配置
│   │   └── Valkyrie.Formatter.csproj
│   ├── Valkyrie.PackageManager/              # Legion 包管理器
│   │   ├── Legion.cs                         # 核心入口，三种工作模式
│   │   ├── LegionManifest.cs                 # legion.von 包清单
│   │   ├── LegionsWorkspace.cs               # legions.von 工作区清单
│   │   ├── LegionConfig.cs                   # 全局/项目配置
│   │   ├── LegionConfigDirectory.cs          # .config/legion/ 配置目录
│   │   ├── LegionIgnore.cs                   # legion.ignore 忽略规则
│   │   ├── LockFile.cs                       # lock.von 版本锁定
│   │   ├── PackageCache.cs                   # 包缓存管理
│   │   ├── PackagePublisher.cs               # 包发布
│   │   ├── DependencyResolver.cs             # 依赖解析与冲突检测
│   │   ├── Registry.cs                       # Package 模型与 IRegistry 接口
│   │   ├── Registries.cs                     # npm / jsr / conda 注册表实现
│   │   ├── RegistrySourceManager.cs          # 注册表源管理与健康检查
│   │   ├── ScriptRunner.cs                   # 脚本执行器
│   │   ├── SecurityAudit.cs                  # 安全审计与许可证检查
│   │   ├── SemanticVersion.cs                # 语义化版本（SemVer）
│   │   └── YearlyVersion.cs                  # 年度版本号（YearlyVersion）
│   └── Valkyrie.Tests/                       # 测试项目
│       ├── LexerTests/ValkyrieLexerTests.cs  # 词法分析器测试
│       ├── ParserTests/ValkyrieParserTests.cs# 语法分析器测试
│       ├── TypeCheckerTests/                 # 类型检查器测试
│       ├── FormatterTests/                   # 格式化器测试
│       └── PackageManagerTests/              # 包管理器测试
├── architecture.md                           # 架构文档
├── legion.md                                 # Legion 包管理器文档
├── vcc.md                                    # VCC 编译器文档
├── vendors.md                                # Vendors 目录结构文档
└── version.md                                # 版本规范文档
```

## 架构总览

```
┌──────────────────────────────────────────────────────────────────┐
│                        用户交互层                                  │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   vcc repl      │  │  vcc build      │  │  vcc test       │ │
│  │   交互式 REPL   │  │  编译为可执行文件 │  │  运行测试       │ │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘ │
│           │                    │                     │           │
│  ┌────────┴────────────────────┴─────────────────────┴────────┐ │
│  │                     VCC 编译器核心                          │ │
│  │  词法分析（Oak.GGScript）→ 语法分析 → AST → IKun IR → 执行  │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                    Legion 包管理器                          │ │
│  │  install / update / remove / resolve / audit / publish     │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐                │ │
│  │  │   npm    │  │   jsr    │  │  conda   │  注册表适配     │ │
│  │  └──────────┘  └──────────┘  └──────────┘                │ │
│  └────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

## 双版本号体系

Valkyrie 同时支持两种版本号格式，分别用于不同场景：

| 版本格式 | 用途 | 格式 | 示例 |
|:---|:---|:---|:---|
| **SemanticVersion** | 外部依赖版本匹配（npm/jsr/conda） | `major.minor.patch[-pre][+build]` | `1.2.3-alpha.1+build.456` |
| **YearlyVersion** | Valkyrie 生态内部版本号 | `yearly.major.minor.patch[-build_info]` | `2024.1.0.0-rc1` |

### YearlyVersion 版本状态

| 状态 | 条件 | 说明 |
|:---|:---|:---|
| 预研版 | `yearly = 0` | 早期探索阶段，不稳定 |
| 测试版 | `yearly > 0` 且 `major = 0` | 正式发布前的测试版本 |
| 稳定版 | `yearly > 0` 且 `major > 0` | 正式发布的稳定版本 |

### 版本范围匹配

**SemanticVersion** 支持 `^`、`~`、`>=`、`<` 等范围运算符：

```csharp
var range = VersionRange.Parse("^1.2.3");
range.Satisfies(SemanticVersion.Parse("1.9.9"));  // true
range.Satisfies(SemanticVersion.Parse("2.0.0"));  // false
```

**YearlyVersion** 使用 `*` 通配符和 `+` 最低版本后缀：

```csharp
var range = YearlyVersionRange.Parse("2024.1.3+");
range.Satisfies(YearlyVersion.Parse("2024.1.2.5"));  // false，有漏洞
range.Satisfies(YearlyVersion.Parse("2024.1.3.0"));  // true，安全
range.Satisfies(YearlyVersion.Parse("2024.2.0.0"));  // true，安全
```

## Legion 三种工作模式

Legion 根据当前目录自动检测工作模式：

| 模式 | 检测条件 | 支持的命令 |
|:---|:---|:---|
| **Workspace** | 存在 `legions.von` | `install`、`update`、`run`、`audit`、`publish` |
| **Package** | 存在 `legion.von` | `install`、`update`、`run`、`add`、`remove`、`audit`、`publish` |
| **Script** | 两者都不存在 | `run`（直接执行命令） |

### Workspace 模式

```
/your-workspace/
├── legions.von              # 工作区清单
├── legion.ignore            # 忽略文件
├── .config/legion/          # 配置目录
├── packages/
│   ├── core/legion.von      # 成员包
│   └── ui/legion.von        # 成员包
├── vendors/                 # 安装的依赖
└── lock.von                 # 版本锁定
```

### Package 模式

```
/your-package/
├── legion.von               # 包清单
├── .config/legion/          # 配置目录
├── src/main.v               # 源码
├── vendors/                 # 安装的依赖
└── lock.von                 # 版本锁定
```

## Valkyrie 语言特性

Valkyrie 语言基于 Oak.GGScript 解析器，为游戏引擎 ECS 架构提供一等公民语法：

### 组件声明

```v
component Position {
    x: f32;
    y: f32;
}
```

### 系统声明

```v
system MovementSystem {
    query all = Query.all(Position, Velocity);

    on_update(frame: Frame) {
        loop entity in query.all {
            entity.position.x += entity.velocity.x * frame.dt
        }
    }
}
```

### 函数声明

```v
micro add(a: i32, b: i32): i32 {
    return a + b
}
```

### 变量声明

```v
let x: i32 = 42;
let mut counter: i32 = 0;
let items: list<i32> = [];
let data: map<string, list<i32>> = {};
```

### 特性标注

```v
[Serializable]
component Tag {
    value: bool;
}

[Export]
micro greet(name: string): string {
    return "Hello, " + name
}
```

### Widget 声明

```v
widget Button {
    label: string;

    render(frame: Frame) {
        return null
    }
}
```

### Plugin 声明

```v
plugin PhysicsEngine {
    requires_arch = ["x86_64", "aarch64"];
    provides_macros = ["PHYSICS_2D", "PHYSICS_3D"];
    provides_capabilities = ["collision", "rigidbody"]
}
```

### 注释

```v
# 行注释
x = 1  # 行尾注释

<# 块注释
   可以嵌套 <# 内部注释 #>
   继续注释 #>
```

### 元代码块

```v
<% 代码块 %>
<%= 表达式 %>
```

## VCC 与 Legion 的职责边界

| 功能 | VCC | Legion |
|:---|:---|:---|
| 编译 `.v` 文件 | ✅ | ❌ |
| 运行 `.v` 文件 | ✅ | ❌ |
| 类型检查 | ✅ | ❌ |
| 格式化代码 | ✅ | ❌ |
| 安装依赖 | ❌ | ✅ |
| 管理版本 | ❌ | ✅ |
| 发布包 | ❌ | ✅ |
| 执行脚本 | ✅（直接运行） | ✅（管理脚本定义） |
| 读取 `legion.von` | ❌ | ✅ |
| 解析 `xxx.config.v` | ❌ | ✅ |

**关键原则：VCC 不知道 Legion 的存在。**

## Vendors 目录结构

安装的包存储在 `vendors/` 目录下，路径格式为 `vendors/<registry>@<endpoint>/<org>@<package>@<version>/`：

```
vendors/
└── npm@npm.js/
    ├── default@lodash@4.17.21/     # 非作用域包，org 为 default
    └── angular@core@16.0.0/        # 作用域包 @angular/core
```

**重要规则：vendors 不递归。** 所有依赖扁平化存储，`vendors/` 中的包如果也包含 `vendors/`，不会继续解析。

## 依赖关系

| 依赖 | 用途 | 来源 |
|:---|:---|:---|
| `Valkyrie.PackageManager` | 包管理功能 | 项目内引用 |
| `Nyar.Language.Valkyrie` | 语言前端（AST、IKun 转换） | `d:\RiderProjects\NyarVM.cs\examples\Nyar.Language.Valkyrie` |
| `Oak.GGScript` | 词法分析、语法分析 | `d:\RiderProjects\Oak.cs` |
| `Newtonsoft.Json` | JSON 序列化/反序列化 | NuGet |
| `System.CommandLine` | 命令行参数解析 | NuGet |
| `xunit` | 单元测试框架 | NuGet |

## 环境变量

| 变量 | 说明 |
|:---|:---|
| `VALKYRIE_HOME` | Legion 全局根目录，包含全局 `vendors/`，优先级最高 |
| `LEGION_REGISTRY` | 默认注册器 |
| `LEGION_OFFLINE` | 离线模式 |
| `VCC_PATH` | VCC 可执行文件路径 |
| `VCC_DEBUG` | 启用调试输出 |

## 详细文档

所有文档已整合至 [documentation/](documentation/overview/index.md) 目录，根目录文件为索引页。

| 目录 | 说明 |
|:---|:---|
| [documentation/overview/](documentation/overview/index.md) | 概述（项目介绍、设计哲学、快速开始） |
| [documentation/core-systems/](documentation/core-systems/vcc.md) | 核心系统（VCC、运行时、类型检查器、包管理器） |
| [documentation/language/](documentation/language/index.md) | 语言参考（核心语言 + ECS/Schema/Template/Shader/Neural 扩展） |
| [documentation/toolchain/](documentation/toolchain/index.md) | 工具链（VCC/Legion 命令参考、格式化器、检查器、版本/Vendors 规范） |
| [documentation/development/](documentation/development/getting-started.md) | 开发（开发指南、API 参考） |
| [documentation/maintenance/](documentation/maintenance/index.md) | 维护者（架构详解、编码规范、测试指南、依赖规则） |

根目录索引文件（内容已迁移至 documentation/）：

| 文件 | 迁移目标 |
|:---|:---|
| [architecture.md](architecture.md) | [maintenance/architecture.md](documentation/maintenance/architecture.md) |
| [legion.md](legion.md) | [toolchain/legion-reference.md](documentation/toolchain/legion-reference.md) |
| [vcc.md](vcc.md) | [toolchain/vcc-reference.md](documentation/toolchain/vcc-reference.md) |
| [vendors.md](vendors.md) | [toolchain/vendors.md](documentation/toolchain/vendors.md) |
| [version.md](version.md) | [toolchain/version.md](documentation/toolchain/version.md) |

## 开发状态

| 功能 | 状态 |
|:---|:---|
| REPL 交互式环境 | ✅ 已完成 |
| 包安装/卸载/更新 | ✅ 已完成 |
| 依赖解析与冲突检测 | ✅ 已完成 |
| 安全审计与许可证检查 | ✅ 已完成（桩实现） |
| 多注册表支持（npm/jsr/conda） | ✅ 已完成（桩实现） |
| 版本锁定（lock.von） | ✅ 已完成 |
| 脚本执行器 | ✅ 已完成 |
| 包发布 | ✅ 已完成（桩实现） |
| 词法分析器测试 | ✅ 已完成 |
| 语法分析器测试 | ✅ 已完成 |
| 完整语言功能 | 🚧 进行中 |
| 注册表 API 实际调用 | 🚧 进行中 |
| 漏洞数据库对接 | 📋 计划中 |

## 团队

- **团队**: nyar-team
- **代码仓库**: `d:\RiderProjects\Valkyrie.cs\`
