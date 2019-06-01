# VCC（Valkyrie Compiler Collection）

VCC 是 Valkyrie 语言的编译器和运行时入口，提供 REPL 交互环境、包安装和依赖解析命令。

> VCC 不知道 Legion 的存在。VCC 只关心 `.v` 源文件，不读取 `legion.von` 或 `legions.von`。

## 项目结构

```
Valkyrie/
├── Program.cs        # 命令行入口（repl, install, resolve）
├── Readme.md         # 本文档
└── Valkyrie.csproj   # 项目配置（输出程序名 vcc）
```

## 命令行用法

### REPL 交互式环境

```bash
vcc repl
```

进入交互式解释器，输入 Valkyrie 代码实时解析并转换为 IKun IR：

```
Valkyrie REPL
Type 'exit' to quit

>>> let x = 1 + 2
IKun: ...
```

输入 `exit` 退出 REPL。

### 安装包

```bash
vcc install <package>
```

通过 Legion 安装指定包，默认从 npm 注册表安装最新版本。

### 解析依赖

```bash
vcc resolve
```

解析当前项目的依赖关系（待实现）。

## 技术架构

### 编译流程

```
源码 (.v) → 词法分析 (Oak.GGScript) → Token 流 → 语法分析 → AST → ValkyrieToIkunConverter → IKun IR → 执行
```

### 依赖关系

| 依赖 | 用途 | 来源 |
|:---|:---|:---|
| `Valkyrie.PackageManager` | 包安装和依赖解析 | 项目内引用 |
| `Nyar.Language.Valkyrie` | ValkyrieParser + ValkyrieToIkunConverter | NyarVM.cs |
| `Newtonsoft.Json` | JSON 处理 | NuGet |
| `System.CommandLine` | 命令行参数解析 | NuGet |

## 与 Legion 的关系

VCC 的 `install` 和 `resolve` 命令内部调用 `Legion` 类实现，但 VCC 本身不读取 Legion 的配置文件。详细文档参见：

- [vcc.md](../Valkyrie.PackageManager/vcc.md) - VCC 编译器完整文档
- [legion.md](../Valkyrie.PackageManager/legion.md) - Legion 包管理器完整文档

## 开发状态

| 功能 | 状态 |
|:---|:---|
| REPL 交互式环境 | ✅ 已完成 |
| 包安装命令 | ✅ 已完成 |
| 依赖解析命令 | 🚧 进行中 |
| 编译命令（build） | 📋 计划中 |
| 测试命令（test） | 📋 计划中 |
