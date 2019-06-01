# 🔵 std.adaptor.dotnet

Valkyrie .NET 平台 SDK — 提供 .NET 运行时 API 绑定，编译为 CLR 程序集（`.dll`/`.exe`）。

## 📋 目标三元组

| 目标三元组 | 架构 | 供应商 | OS | ABI | 说明 |
|:---|:---|:---|:---|:---|:---|
| `x86_64-pc-windows-msvc` | x86_64 | pc | windows | msvc | Windows x64 .NET（主流） |
| `aarch64-pc-windows-msvc` | aarch64 | pc | windows | msvc | Windows ARM64 .NET |
| `x86_64-unknown-linux-gnu` | x86_64 | unknown | linux | gnu | Linux x64 .NET |
| `aarch64-unknown-linux-gnu` | aarch64 | unknown | linux | gnu | Linux ARM64 .NET |
| `x86_64-apple-darwin` | x86_64 | apple | darwin | — | macOS Intel .NET |
| `aarch64-apple-darwin` | aarch64 | apple | darwin | — | macOS Apple Silicon .NET |

> **三元组格式**：`<arch>-<vendor>-<os>-<abi>`（遵循 LLVM/Rust 标准）
> .NET 是跨平台运行时，同一 IL 代码可在所有平台运行，三元组主要影响 P/Invoke 调用约定。

### 映射到 Nyar CompilationTarget

| 三元组 | Arch | ABI | API | OS | Environment |
|:---|:---|:---|:---|:---|:---|
| `x86_64-pc-windows-msvc` | Clr | CLR | NET | Windows | Native |
| `x86_64-unknown-linux-gnu` | Clr | CLR | NET | Linux | Native |
| `aarch64-apple-darwin` | Clr | CLR | NET | macOS | Native |

### 与其他目标的区别

| 特性 | `wasm32-unknown-unknown` | `wasm32-wasi-preview1` | CLR |
|:---|:---|:---|:---|
| 📦 产出格式 | `.wasm` | `.wasm` | `.dll` / `.exe` |
| 🏗️ 运行时 | 浏览器 JS | Wasmtime/Wasmer | .NET Runtime |
| 📁 文件系统 | ❌ | ✅ | ✅ `System.IO` |
| 🌍 网络 | ❌（仅 `fetch`） | ✅ | ✅ `System.Net` |
| 📊 反射 | ❌ | ❌ | ✅ `System.Reflection` |
| 🔗 互操作 | JS glue | WASI imports | P/Invoke |
| 🧵 多线程 | ❌（SharedArrayBuffer） | ✅ | ✅ `System.Threading` |
| 📚 标准库 | Web API | WASI | BCL（基础类库） |

## 📦 包内容

```
std.adaptor.dotnet/
├── legion.von              # 包清单
├── README.md               # 本文件
└── source/
    ├── console.v           # 控制台 API（Console.Write/WriteLine/ReadLine）
    ├── io.v                # 文件系统 API（File/Directory/Path）
    ├── net.v               # 网络 API（HttpClient）
    ├── math.v              # 数学 API（System.Math）
    ├── string.v            # 字符串 API（String 操作）
    └── threading.v         # 线程 API（Task/Thread）
```

## 🔧 绑定概览

### `[clr("Namespace.Type", "Method")]` 属性

.NET 方法通过 `[clr]` 属性声明，编译时映射为 CLR 方法引用（MemberRef）：

```v
[clr("System.Console", "WriteLine")]
micro console_write_line(value: string): void

[clr("System.IO.File", "ReadAllText")]
micro file_read_all_text(path: string): string

[clr("System.Math", "Abs")]
micro math_abs(value: f64): f64
```

### 文件说明

| 文件 | 绑定类型 | 副作用 | 覆盖 API |
|:---|:---|:---|:---|
| `console.v` | `[clr]` | ✅ 有 | 控制台输入输出 |
| `io.v` | `[clr]` | ✅ 有 | 文件/目录操作 |
| `net.v` | `[clr]` | ✅ 有 | HTTP 请求 |
| `math.v` | `[clr]` | ❌ 纯函数 | 数学函数 |
| `string.v` | `[clr]` | ❌ 纯函数 | 字符串操作 |
| `threading.v` | `[clr]` | ✅ 有 | 异步/线程 |

## 🎯 使用场景

- 🖥️ Windows 桌面应用（WPF/WinForms 后端逻辑）
- 🌐 ASP.NET 服务端
- 🔧 命令行工具
- 🎮 Unity 游戏脚本（通过 CLR 后端）

## ⚙️ 编译命令

```bash
# 编译为 .NET 程序集
vcc build --target clr

# 运行产出
dotnet module.dll
```

## 📌 状态

🚧 占坑阶段，.NET BCL API 绑定待实现完整覆盖。
⚠️ ClrEncoder 的 Blob Heap 和 UserString Heap 编码为最小实现（返回 `[0]`），暂不支持字段签名和字符串字面量。
⚠️ ClrTypeMapper.MapToMethodAttributes 始终返回 `Public | Static`，待完善访问修饰符映射。
