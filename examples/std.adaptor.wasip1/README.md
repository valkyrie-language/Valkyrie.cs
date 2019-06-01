# 🐚 std.adaptor.wasip1

Valkyrie WASI Preview 1 平台 SDK — 提供 WASI Preview 1 系统调用绑定，编译为 `.wasm` 运行于 WASI 兼容运行时。

## 📋 目标三元组

| 目标三元组 | 架构 | 供应商 | OS | ABI | 说明 |
|:---|:---|:---|:---|:---|:---|
| `wasm32-wasi-preview1` | wasm32 | wasi | wasi | preview1 | WASI Preview 1（Wasmtime/Wasmer） |

> **三元组格式**：`<arch>-<vendor>-<os>-<abi>`（遵循 LLVM/Rust 标准）
> WASI Preview 1 基于文件描述符的 Capability-based 安全模型，所有系统资源通过 fd 访问。

### 映射到 Nyar CompilationTarget

| 三元组 | Arch | ABI | API | OS | Environment |
|:---|:---|:---|:---|:---|:---|
| `wasm32-wasi-preview1` | Wasm32 | WasiP1 | POSIX | Web | Native |

### 与其他目标的区别

| 特性 | CLR | JVM | WASI P1 | WASI P2 |
|:---|:---|:---|:---|:---|
| 📦 产出格式 | `.dll` / `.exe` | `.class` | `.wasm` | `.wasm` |
| 🏗️ 运行时 | .NET Runtime | JVM | Wasmtime/Wasmer | Wasmtime (Component) |
| 📁 文件系统 | ✅ `System.IO` | ✅ `java.io` | ✅ `fd_read`/`fd_write` | ✅ 组件化接口 |
| 🌍 网络 | ✅ `System.Net` | ✅ `java.net` | ✅ `sock_connect` | ✅ 组件化接口 |
| 🌐 HTTP | ✅ `HttpClient` | ✅ `HttpURLConnection` | ❌ | ✅ `wasi:http` |
| 🔗 互操作 | P/Invoke | JNI | WASI imports | Component Model |
| 🧵 多线程 | ✅ | ✅ | ❌ | ❌ |
| 📚 标准库 | BCL | JDK | WASI minimal | WASI Component |

## 📦 包内容

```
std.adaptor.wasip1/
├── legion.von              # 包清单
├── README.md               # 本文件
└── source/
    ├── args.v              # 命令行参数（args_get/args_sizes_get）
    ├── clock.v             # 时钟（clock_time_get/clock_res_get）
    ├── environ.v           # 环境变量（environ_get/environ_sizes_get）
    ├── fs.v                # 文件系统（path_open/path_rename/path_create_directory 等）
    ├── io.v                # I/O（fd_write/fd_read/fd_close/fd_seek/fd_tell 等）
    ├── poll.v              # 事件轮询（poll_oneoff）
    ├── proc.v              # 进程（proc_exit）
    ├── random.v            # 随机数（random_get）
    └── sock.v              # 网络套接字（sock_connect/sock_send/sock_recv/sock_shutdown）
```

## 🔧 绑定概览

### `[wasi("function_name")]` 属性

WASI 函数通过 `[wasi]` 属性声明，编译时映射为 WASI 导入函数：

```v
[wasi("fd_write")]
micro wasi_fd_write(fd: i32, iovs_ptr: i32, iovs_len: i32, nwritten_ptr: i32): i32

[wasi("path_open")]
micro wasi_path_open(fd: i32, dirflags: i32, path_ptr: i32, path_len: i32, oflags: i32, fs_rights_base: i64, fs_rights_inheriting: i64, fdflags: i32, opened_fd_ptr: i32): i32

[wasi("random_get")]
micro wasi_random_get(buf_ptr: i32, buf_len: i32): i32
```

### 文件说明

| 文件 | 绑定类型 | 副作用 | 覆盖 API |
|:---|:---|:---|:---|
| `args.v` | `[wasi]` | ✅ 有 | 命令行参数获取 |
| `clock.v` | `[wasi]` | ❌ 纯查询 | 时钟时间/精度查询 |
| `environ.v` | `[wasi]` | ✅ 有 | 环境变量获取 |
| `fs.v` | `[wasi]` | ✅ 有 | 文件系统操作（打开/重命名/删除/创建目录/链接/符号链接） |
| `io.v` | `[wasi]` | ✅ 有 | 文件描述符读写/关闭/定位/预取状态 |
| `poll.v` | `[wasi]` | ✅ 有 | 事件轮询 |
| `proc.v` | `[wasi]` | ✅ 有 | 进程退出 |
| `random.v` | `[wasi]` | ✅ 有 | 随机数生成 |
| `sock.v` | `[wasi]` | ✅ 有 | TCP 套接字连接/发送/接收/关闭 |

## 🎯 使用场景

- 🖥️ 服务端 Wasm 应用（Wasmtime/Wasmer 运行时）
- 🔧 命令行工具（WASI 沙箱化执行）
- 🐳 容器化微服务（轻量级 Wasm 替代 Docker）
- 🔒 安全沙箱（Capability-based 权限模型）

## ⚙️ 编译命令

```bash
# 编译为 WASI Preview 1 模块
vcc build --target wasip1

# 运行产出
wasmtime module.wasm
wasmer module.wasm
```

## 📌 状态

🚧 占坑阶段，WASI Preview 1 API 绑定已覆盖 9 个模块，包含 30+ 系统调用。
⚠️ WASI Preview 1 无 HTTP 支持，如需 HTTP 请使用 `std.adaptor.wasip2`。
