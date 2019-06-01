# 🐧 std.adaptor.linux

Valkyrie Linux 平台 SDK — 提供 Linux 原生 API 绑定。

## 📋 目标三元组

| 目标三元组 | 架构 | 供应商 | OS | ABI/环境 | 说明 |
|:---|:---|:---|:---|:---|:---|
| `x86_64-pc-linux-gnu` | x86_64 | pc | linux | gnu | 64 位 x86 Linux（主流桌面/服务器） |
| `aarch64-unknown-linux-gnu` | aarch64 | unknown | linux | gnu | 64 位 ARM Linux（树莓派、嵌入式） |
| `riscv64-unknown-linux-gnu` | riscv64 | unknown | linux | gnu | RISC-V 64 位 Linux（实验性） |
| `i686-pc-linux-gnu` | x86 | pc | linux | gnu | 32 位 x86 Linux（旧硬件） |

> **三元组格式**：`<arch>-<vendor>-<os>[-<abi>]`（遵循 LLVM/Rust 标准）
> - **arch**: 目标架构（x86_64、aarch64、riscv64）
> - **vendor**: 平台供应商（pc=通用 PC、unknown=未知/通用）
> - **os**: 操作系统（linux）
> - **abi**: 应用二进制接口（gnu=GNU libc）

### 映射到 Nyar CompilationTarget

| 三元组 | Arch | ABI | API | OS | Environment |
|:---|:---|:---|:---|:---|:---|
| `x86_64-pc-linux-gnu` | X86_64 | SystemV | POSIX | Linux | Native |
| `aarch64-unknown-linux-gnu` | AArch64 | AAPCS64 | POSIX | Linux | Native |
| `riscv64-unknown-linux-gnu` | RiscV64 | LP64D | POSIX | Linux | Native |

## 📦 包内容

```
std.adaptor.linux/
├── legion.von          # 包清单
└── source/
    └── posix.v         # POSIX API 绑定
```

## 🔧 绑定概览

| 文件 | 绑定类型 | 覆盖 API |
|:---|:---|:---|
| `posix.v` | `[c]` / `[syscall]` | 文件描述符（`write`/`read`/`close`）、文件系统、内存、进程、网络 |

### POSIX API

通过 `[c]` 属性绑定 `libc` 共享库函数（C 调用约定）：

```v
[c("libc", "write")]
micro posix_write(fd: i32, buf: c_str, len: i32): i32

[c("libc", "read")]
micro posix_read(fd: i32, buf: c_str, len: i32): i32

[c("libc", "close")]
micro posix_close(fd: i32): i32
```

通过 `[syscall]` 属性直接发起 Linux 内核系统调用（绕过 libc）：

```v
[syscall(1)]
micro sys_write(fd: i32, buf: c_str, len: i32): i64

[syscall(0)]
micro sys_read(fd: i32, buf: i32, len: i32): i64
```

### 注解说明

本 SDK 使用以下注解（完整 FFI 注解体系参见 [attributes.md](../../documentation/language/attributes.md)）：

| 注解 | 语义 |
|:---|:---|
| `[c("lib", "func")]` | C 调用约定，绑定 libc/libpthread 共享库函数 |
| `[syscall(number)]` | 直接 Linux 内核系统调用，绕过 libc |

## 🎯 使用场景

- 🖥️ 桌面游戏 Linux 原生端口
- 🏗️ 服务器端游戏逻辑（Dedicated Server）
- 🔧 嵌入式 Linux 游戏设备

## ⚙️ 编译命令

```bash
# 编译为 NyarVM（默认）
vcc build --target nyar

# 编译为 WASM（通过 WASM 中间表示）
vcc build --target wasm

# 指定架构和 OS
vcc build --arch x86_64 --os linux
```

## 📌 状态

🚧 占坑阶段，待实现完整 POSIX API 覆盖（文件系统、网络、信号、线程等）。
