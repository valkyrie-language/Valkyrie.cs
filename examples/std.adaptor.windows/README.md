# 🪟 std.adaptor.windows

Valkyrie Windows 平台 SDK — 提供 Windows 原生 API 绑定。

## 📋 目标三元组

| 目标三元组 | 架构 | 供应商 | OS | ABI | 说明 |
|:---|:---|:---|:---|:---|:---|
| `x86_64-pc-windows-msvc` | x86_64 | pc | windows | msvc | 64 位 Windows（主流桌面/游戏） |
| `aarch64-pc-windows-msvc` | aarch64 | pc | windows | msvc | ARM64 Windows（Surface Pro X、Copilot+ PC） |
| `i686-pc-windows-msvc` | x86 | pc | windows | msvc | 32 位 Windows（旧硬件/兼容） |

> **三元组格式**：`<arch>-<vendor>-<os>-<abi>`（遵循 LLVM/Rust 标准）
> - **vendor**: `pc`（通用 PC 平台）
> - **os**: `windows`
> - **abi**: `msvc`（Microsoft Visual C++ ABI，涵盖所有 Windows 调用约定）

### 映射到 Nyar CompilationTarget

| 三元组 | Arch | ABI | API | OS | Environment |
|:---|:---|:---|:---|:---|:---|
| `x86_64-pc-windows-msvc` | X86_64 | MicrosoftX64 | Windows | Windows | Native |
| `aarch64-pc-windows-msvc` | AArch64 | MicrosoftArm64 | Windows | Windows | Native |
| `i686-pc-windows-msvc` | X86 | MicrosoftX86 | Windows | Windows | Native |

> **Windows ABI 说明**：
> - x86_64: Microsoft x64 调用约定（与 SystemV AMD64 不同）
> - aarch64: Microsoft ARM64 调用约定（基于 AAPCS64 但有重大修改）
> - x86: MSVC 32 位调用约定（包含 cdecl、stdcall、thiscall）

## 📦 包内容

```
std.adaptor.windows/
├── legion.von          # 包清单
└── source/
    └── win32.v         # Win32 API 绑定
```

## 🔧 绑定概览

| 文件 | 绑定类型 | 覆盖 API |
|:---|:---|:---|
| `win32.v` | `[c]` / `[com]` / `[syscall]` | 窗口（MessageBox）、控制台（StdHandle/WriteConsole）、COM、NT 系统调用 |

### Win32 API

通过 `[c]` 属性绑定 Windows 系统 DLL 导出函数（C 调用约定）：

```v
# 窗口
[c("user32", "MessageBoxW")]
micro win_message_box(title: utf16, text: utf16): i32

# 控制台
[c("kernel32", "GetStdHandle")]
micro win_get_std_handle(nstd: i32): i32

[c("kernel32", "WriteConsoleW")]
micro win_write_console(handle: i32, buf: utf16, len: i32): i32
```

通过 `[com]` 属性绑定 COM 接口方法（vtable 调用）：

```v
[com("IUnknown", "Release")]
micro com_release(this: i32): i32
```

通过 `[syscall]` 属性绑定 NT 系统调用（绕过 Win32 子系统）：

```v
[syscall("NtClose")]
micro nt_close(handle: i32): i32
```

### 注解说明

本 SDK 使用以下注解（完整 FFI 注解体系参见 [attributes.md](../../documentation/language/attributes.md)）：

| 注解 | 语义 |
|:---|:---|
| `[c("lib", "func")]` | C 调用约定（cdecl/stdcall），绑定 DLL 导出函数 |
| `[com("Interface", "Method")]` | COM vtable 调用，绑定接口方法 |
| `[syscall(number)]` | 直接 NT 系统调用，绕过 Win32 子系统 |

## 🎯 使用场景

- 🎮 Windows 桌面游戏（Steam/微软商店）
- 🖥️ Win32 原生窗口管理
- 📟 控制台工具开发

## ⚙️ 编译命令

```bash
# 编译为 NyarVM（默认）
vcc build --target nyar

# 编译为 CLR（.NET 运行时）
vcc build --target clr

# 指定 Windows 目标
vcc build --arch x86_64 --os windows
```

## 📌 状态

🚧 占坑阶段，待实现完整 Win32 API 覆盖（GDI、DirectX、文件系统、注册表、COM 等）。
