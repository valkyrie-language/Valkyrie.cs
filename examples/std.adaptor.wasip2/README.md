# 🧩 std.adaptor.wasip2

Valkyrie WASI Preview 2 (Component Model) 平台 SDK — 提供组件化 WASI 接口绑定，编译为 `.wasm` 组件运行于 WASI Preview 2 兼容运行时。

## 📋 目标三元组

| 目标三元组 | 架构 | 供应商 | OS | ABI | 说明 |
|:---|:---|:---|:---|:---|:---|
| `wasm32-wasi-preview2` | wasm32 | wasi | wasi | preview2 | WASI Preview 2（Component Model） |

> **三元组格式**：`<arch>-<vendor>-<os>-<abi>`（遵循 LLVM/Rust 标准）
> WASI Preview 2 基于 WebAssembly Component Model，使用结构化接口替代 P1 的平面 fd 接口。

### 映射到 Nyar CompilationTarget

| 三元组 | Arch | ABI | API | OS | Environment |
|:---|:---|:---|:---|:---|:---|
| `wasm32-wasi-preview2` | Wasm32 | WasiP2 | POSIX | Web | Native |

### 与 WASI Preview 1 的区别

| 特性 | WASI P1 | WASI P2 |
|:---|:---|:---|
| 🏗️ 接口模型 | 平面 fd 接口 | Component Model（结构化） |
| 📁 文件系统 | `fd_read`/`fd_write`/`path_open` | `wasi:filesystem` 组件接口 |
| 🌍 网络 | `sock_connect`/`sock_send`/`sock_recv` | `wasi:sockets` 组件接口 |
| 🌐 HTTP | ❌ | ✅ `wasi:http` 组件接口 |
| ⏰ 时钟 | `clock_time_get` | `wasi:clocks` 组件接口 |
| 📋 CLI | `args_get`/`environ_get` | `wasi:cli` 组件接口 |
| 🔗 类型系统 | 仅 `i32`/`i64` 指针 | 结构化类型（元组、列表、字符串） |
| 📦 产出格式 | 核心模块 `.wasm` | 组件 `.wasm` |

## 📦 包内容

```
std.adaptor.wasip2/
├── legion.von              # 包清单
├── README.md               # 本文件
└── source/
    ├── cli.v               # CLI 环境/退出/标准流（wasi:cli）
    ├── clock.v             # 时钟（wasi:clocks — monotonic/wall-clock）
    ├── fs.v                # 文件系统（wasi:filesystem — open-at/create/remove）
    ├── http.v              # HTTP 客户端/服务端（wasi:http — P2 独有）
    ├── io.v                # I/O 流（wasi:io/streams — read/write/skip）
    ├── poll.v              # 事件轮询（wasi:io/poll — poll/ready/block）
    ├── random.v            # 随机数（wasi:random — secure/insecure）
    └── sock.v              # 网络套接字（wasi:sockets — TCP/UDP/DNS）
```

## 🔧 绑定概览

### `[wasi_p2("wasi:namespace/interface", "method")]` 属性

WASI P2 函数通过 `[wasi_p2]` 属性声明，编译时映射为 Component Model 导入：

```v
[wasi_p2("wasi:cli/environment", "get-arguments")]
extern wasi_p2_cli_get_arguments(): [utf8]

[wasi_p2("wasi:io/streams", "read")]
extern wasi_p2_stream_read(stream: i32, len: i64): ([u8], i32)

[wasi_p2("wasi:http/outgoing-handler", "handle")]
extern wasi_p2_http_outgoing_handle(request: i32): (i32, i32)
```

### 文件说明

| 文件 | 绑定类型 | 副作用 | 覆盖 API |
|:---|:---|:---|:---|
| `cli.v` | `[wasi_p2]` | ✅ 有 | 环境变量/命令行参数/退出/标准流/终端检测 |
| `clock.v` | `[wasi_p2]` | ❌ 纯查询 | 单调时钟/挂钟（now/resolution/subscribe） |
| `fs.v` | `[wasi_p2]` | ✅ 有 | 文件系统操作（open-at/create/remove/rename） |
| `http.v` | `[wasi_p2]` | ✅ 有 | HTTP 请求/响应/Headers（P2 独有） |
| `io.v` | `[wasi_p2]` | ✅ 有 | I/O 流读写/跳过/阻塞读写 |
| `poll.v` | `[wasi_p2]` | ✅ 有 | 事件轮询/就绪检测/阻塞 |
| `random.v` | `[wasi_p2]` | ✅ 有 | 安全/非安全随机数 |
| `sock.v` | `[wasi_p2]` | ✅ 有 | TCP/UDP 套接字/DNS 解析 |

## 🎯 使用场景

- 🌐 HTTP 微服务（WASI P2 原生 HTTP 支持）
- 🧩 Wasm 组件插件系统（Component Model 互操作）
- 🖥️ 服务端 Wasm 应用（Wasmtime Component 运行时）
- 🔒 安全沙箱（Capability-based + Component Model 权限）

## ⚙️ 编译命令

```bash
# 编译为 WASI Preview 2 组件
vcc build --arch wasm32 --os wasi --abi wasip2

# 运行产出
wasmtime --wasm component-model module.wasm
```

## 📌 状态

🚧 占坑阶段，WASI Preview 2 API 绑定已覆盖 8 个模块，包含 50+ 组件接口。
✅ HTTP 绑定已实现（P2 相对于 P1 的核心新增能力）。
