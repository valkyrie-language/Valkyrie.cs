# 🍎 std.adaptor.macos

Valkyrie macOS 平台 SDK — 提供 macOS/iOS 原生 API 绑定。

## 📋 目标三元组

| 目标三元组 | 架构 | 供应商 | OS | ABI/环境 | 说明 |
|:---|:---|:---|:---|:---|:---|
| `aarch64-apple-darwin` | aarch64 | apple | darwin | — | Apple Silicon Mac（M1/M2/M3/M4） |
| `x86_64-apple-darwin` | x86_64 | apple | darwin | — | Intel Mac（旧款） |
| `aarch64-apple-ios` | aarch64 | apple | ios | — | iPhone / iPad 真机 |
| `x86_64-apple-ios-sim` | x86_64 | apple | ios | sim | iOS 模拟器（Intel） |
| `aarch64-apple-ios-sim` | aarch64 | apple | ios | sim | iOS 模拟器（Apple Silicon） |

> **三元组格式**：`<arch>-<vendor>-<os>[-<abi>]`（遵循 LLVM/Rust 标准）
> - **vendor**: `apple`（Apple 平台专用）
> - **os**: `darwin`（macOS 内核名）或 `ios`
> - **sim**: 表示模拟器环境（非 ABI，是环境后缀）

### 映射到 Nyar CompilationTarget

| 三元组 | Arch | ABI | API | OS | Environment |
|:---|:---|:---|:---|:---|:---|
| `aarch64-apple-darwin` | AArch64 | AAPCS64 | POSIX | macOS | Native |
| `x86_64-apple-darwin` | X86_64 | SystemV | POSIX | macOS | Native |
| `aarch64-apple-ios` | AArch64 | AAPCS64 | POSIX | iOS | Mobile |
| `x86_64-apple-ios-sim` | X86_64 | SystemV | POSIX | iOS | Mobile |

> **注意**：iOS 模拟器的 ABI 与目标架构匹配（x86_64 用 SystemV，aarch64 用 AAPCS64），与真机相同。

## 📦 包内容

```
std.adaptor.macos/
├── legion.von          # 包清单
└── source/
    └── darwin.v        # Darwin API 绑定
```

## 🔧 绑定概览

| 文件 | 绑定类型 | 覆盖 API |
|:---|:---|:---|
| `darwin.v` | `[c]` / `[syscall]` | Foundation 框架（`NSLog`）、libSystem、Core Foundation、Core Graphics、ObjC Runtime |

### Darwin API

通过 `[c]` 属性绑定 macOS/iOS 原生框架（C 调用约定）：

```v
[c("Foundation", "NSLog")]
micro darwin_ns_log(format: utf8): i32
```

通过 `[syscall]` 属性绑定 Darwin/Mach 内核系统调用：

```v
[syscall(4)]
micro sys_darwin_write(fd: i32, buf: c_str, len: i32): i64
```

### 注解说明

本 SDK 使用以下注解（完整 FFI 注解体系参见 [attributes.md](../../documentation/language/attributes.md)）：

| 注解 | 语义 |
|:---|:---|
| `[c("lib", "func")]` | C 调用约定，绑定 Framework/libSystem 共享库函数 |
| `[syscall(number)]` | 直接 Darwin/Mach 内核系统调用 |

## 🎯 使用场景

- 🎮 macOS 原生游戏发布
- 📱 iOS 游戏移植
- 🖥️ Apple Silicon 原生优化

## ⚙️ 编译命令

```bash
# 编译为 NyarVM（默认）
vcc build --target nyar

# 指定 macOS 目标
vcc build --arch aarch64 --os macos

# 指定 iOS 目标
vcc build --arch aarch64 --os ios
```

## 📌 状态

🚧 占坑阶段，待实现完整 Darwin API 覆盖（Foundation、AppKit、UIKit、Metal、CoreGraphics 等）。
