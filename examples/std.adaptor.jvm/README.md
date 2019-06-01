# ☕ std.adaptor.jvm

Valkyrie JVM 平台 SDK — 提供 Java 运行时 API 绑定，编译为 JVM 字节码（`.class`）。

## 📋 目标三元组

| 目标三元组 | 架构 | 供应商 | OS | ABI | 说明 |
|:---|:---|:---|:---|:---|:---|
| `jvm-unknown-linux-gnu` | jvm | unknown | linux | gnu | Linux x64 JVM（OpenJDK/Oracle） |
| `jvm-unknown-darwin` | jvm | unknown | darwin | — | macOS JVM |
| `jvm-unknown-windows-msvc` | jvm | unknown | windows | msvc | Windows JVM |
| `jvm-unknown-android` | jvm | unknown | android | — | Android Dalvik/ART |

> **三元组格式**：`<arch>-<vendor>-<os>-<abi>`（遵循 LLVM/Rust 标准）
> JVM 是跨平台运行时，同一字节码可在所有平台运行，三元组主要影响 JNI 调用约定。
> JVM 目标的三元组中 `arch` 固定为 `jvm`，因为 JVM 字节码与物理架构无关。

### 映射到 Nyar CompilationTarget

| 三元组 | Arch | ABI | API | OS | Environment |
|:---|:---|:---|:---|:---|:---|
| `jvm-unknown-linux-gnu` | Jvm | JVM | Java | Linux | Native |
| `jvm-unknown-darwin` | Jvm | JVM | Java | macOS | Native |
| `jvm-unknown-windows-msvc` | Jvm | JVM | Java | Windows | Native |

### 与其他目标的区别

| 特性 | CLR | JVM | WASI |
|:---|:---|:---|:---|
| 📦 产出格式 | `.dll` / `.exe` | `.class` | `.wasm` |
| 🏗️ 运行时 | .NET Runtime | JVM (OpenJDK) | Wasmtime |
| 📁 文件系统 | ✅ `System.IO` | ✅ `java.io` / `java.nio` | ✅ WASI fd |
| 🌍 网络 | ✅ `System.Net` | ✅ `java.net` | ✅ WASI sock |
| 📊 反射 | ✅ | ✅ | ❌ |
| 🔗 互操作 | P/Invoke | JNI | WASI imports |
| 🧵 多线程 | ✅ | ✅ | ❌ |
| 📚 标准库 | BCL | JDK | WASI minimal |

## 📦 包内容

```
std.adaptor.jvm/
├── legion.von              # 包清单
├── README.md               # 本文件
└── source/
    ├── console.v           # 控制台 API（System.out/in）
    ├── io.v                # 文件系统 API（java.io.File）
    ├── net.v               # 网络 API（java.net.URL/HttpURLConnection）
    ├── math.v              # 数学 API（java.lang.Math）
    ├── string.v            # 字符串 API（java.lang.String）
    └── thread.v            # 线程 API（java.lang.Thread）
```

## 🔧 绑定概览

### `[jvm("fully.qualified.Class", "methodName")]` 属性

Java 方法通过 `[jvm]` 属性声明，编译时映射为 JVM 常量池方法引用（Methodref）：

```v
[jvm("java.lang.System", "out.println")]
micro jvm_println(value: string): void

[jvm("java.lang.Math", "abs")]
micro jvm_math_abs(value: f64): f64

[jvm("java.io.File", "exists")]
micro jvm_file_exists(path: string): bool
```

### 文件说明

| 文件 | 绑定类型 | 副作用 | 覆盖 API |
|:---|:---|:---|:---|
| `console.v` | `[jvm]` | ✅ 有 | 控制台输入输出 |
| `io.v` | `[jvm]` | ✅ 有 | 文件/目录操作 |
| `net.v` | `[jvm]` | ✅ 有 | HTTP 请求 |
| `math.v` | `[jvm]` | ❌ 纯函数 | 数学函数 |
| `string.v` | `[jvm]` | ❌ 纯函数 | 字符串操作 |
| `thread.v` | `[jvm]` | ✅ 有 | 线程操作 |

## 🎯 使用场景

- 🖥️ 服务端 Java 应用（Spring Boot 后端逻辑）
- 📱 Android 游戏脚本
- 🔧 大数据处理（Hadoop/Spark UDF）
- 🎮 Minecraft 插件（Bukkit/Spigot）

## ⚙️ 编译命令

```bash
# 编译为 JVM 字节码
vcc build --target jvm

# 运行产出
java Module
```

## 📌 状态

🚧 占坑阶段，JDK API 绑定待实现完整覆盖。
⚠️ JvmBackend 的 `EmitCallFromInstruction` 写入 methodref 索引为 0（占位符），跨函数调用尚未实现常量池解析。
⚠️ JvmBackend 的 `EmitJumpFromInstruction` 写入跳转偏移为 0（占位符），分支偏移尚未计算。
⚠️ Acorn.Jvm.Decode 的属性解码为 stub（始终返回 null），无法完整 round-trip。
