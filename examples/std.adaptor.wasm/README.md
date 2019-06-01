# 🌐 std.adaptor.wasm

Valkyrie WebAssembly 平台 SDK — 提供 Web 浏览器 API 绑定，覆盖完整 Web API 生态。

## 📋 目标三元组

| 目标三元组 | 架构 | 供应商 | OS | ABI/环境 | 说明 |
|:---|:---|:---|:---|:---|:---|
| `wasm32-unknown-unknown` | wasm32 | unknown | unknown | — | 纯浏览器环境，仅 DOM/JS API |
| `wasm32-wasi-preview1` | wasm32 | wasi | wasi | preview1 | WASI Preview 1（文件系统/网络访问） |
| `wasm32-wasi-preview2` | wasm32 | wasi | wasi | preview2 | WASI Preview 2（组件模型） |

> **三元组格式**：`<arch>-<vendor>-<os>[-<abi>]`（遵循 LLVM/Rust 标准）
> - **arch**: `wasm32`（WebAssembly 32 位）
> - **vendor**: `unknown`（浏览器）或 `wasi`（WASI 运行时）
> - **os**: `unknown`（浏览器无 OS）或 `wasi`（WASI 系统接口）

### 三种目标的区别

| 特性 | `wasm32-unknown-unknown` | `wasm32-wasi-preview1` | `wasm32-wasi-preview2` |
|:---|:---|:---|:---|
| 🌐 浏览器 DOM API | ✅ | ❌ | ❌ |
| 📁 文件系统 | ❌ | ✅ `fd_read`/`fd_write` | ✅ 组件化接口 |
| 🌍 网络 | ❌（仅 `fetch`） | ✅ `sock_connect` | ✅ 组件化接口 |
| ⏰ 系统时钟 | ❌ | ✅ `clock_time_get` | ✅ 组件化接口 |
| 🔒 沙箱模型 | 浏览器沙箱 | Capability-based | Component Model |
| 📦 适用场景 | Web 游戏/应用 | 服务端 Wasm | 服务端 Wasm（下一代） |

### 映射到 Nyar CompilationTarget

| 三元组 | Arch | ABI | API | OS | Environment |
|:---|:---|:---|:---|:---|:---|
| `wasm32-unknown-unknown` | Wasm32 | WebAssembly | Web | Web | Web |
| `wasm32-wasi-preview1` | Wasm32 | WasiP1 | POSIX | Web | Native |
| `wasm32-wasi-preview2` | Wasm32 | WasiP2 | POSIX | Web | Native |

## 📦 包内容

```
std.adaptor.wasm/
├── legion.von          # 包清单
└── source/
    ├── console.v       # Console API
    ├── crypto.v        # Crypto API
    ├── dom.v           # DOM API
    ├── fetch.v         # Fetch API
    ├── json.v          # JSON API
    ├── math.v          # Math API
    ├── performance.v   # Performance API
    ├── storage.v       # Storage API
    ├── timer.v         # Timer API
    └── url.v           # URL API
```

## 🔧 绑定概览

| 文件 | 绑定类型 | 副作用 | 覆盖 API |
|:---|:---|:---|:---|
| `console.v` | `[js_builtin("console.*")]` | ✅ 有 | 日志、分组、计时 |
| `crypto.v` | `[js_builtin("crypto.*")]` | ✅ 有 | 随机值、UUID |
| `dom.v` | `[js_builtin("document.*"/"Element.*")]` | ✅ 有 | 元素查询/创建/操作/样式/树/事件 |
| `fetch.v` | `[js_builtin("fetch"/"Response.*")]` | ✅ 有 | HTTP 请求/响应/Headers |
| `json.v` | `[js_builtin("JSON.*"), pure]` | ❌ 纯函数 | 解析/序列化 |
| `math.v` | `[js_builtin("Math.*"), pure]` | ❌ 纯函数 | 常量/取整/极值/幂根/对数/三角 |
| `performance.v` | `[js_builtin("performance.*")]` | 混合 | 时间戳（纯）/ 标记（副作用） |
| `storage.v` | `[js_builtin("localStorage.*"/"sessionStorage.*")]` | ✅ 有 | 本地/会话存储 CRUD |
| `timer.v` | `[js_builtin("setTimeout"/"raf")]` | ✅ 有 | 定时器/动画帧/微任务 |
| `url.v` | `[js_builtin("URL*")]` | 混合 | URL 解析/URLSearchParams |

### 绑定类型说明

- **`[js_builtin]`**：直接映射 JS 内置全局对象，编译到 `wasm32-unknown-unknown` 时生成 JS glue 调用
- **`[wasi]`**：绑定 WASI 系统调用，编译到 `wasi-preview1/2` 时通过 WASI 接口调用

### 注解说明

本 SDK 使用以下注解（完整 FFI 注解体系参见 [attributes.md](../../documentation/language/attributes.md)）：

| 注解 | 语义 |
|:---|:---|
| `[js_builtin("path")]` | 直接映射 JS 内置全局对象，编译到 `wasm32-unknown-unknown` 时生成 JS glue 调用 |
| `[wasi]` | 绑定 WASI 系统调用，编译到 `wasi-preview1/2` 时通过 WASI 接口调用 |

### 纯函数标注

标注 `pure` 的函数可被编译器优化（DCE、常量折叠、公共子表达式消除）。

## 🎯 使用场景

- 🎮 Web 游戏发布（`wasm32-unknown-unknown`）
- 🌍 Web 应用开发（SPA、PWA）
- 🖥️ 服务端 Wasm 运行时（`wasi-preview1/2`）
- 🧩 Wasm 插件系统（`wasi-preview2` 组件模型）

## ⚙️ 编译命令

```bash
# 编译为浏览器 WASM（默认）
vcc build --target wasm

# 编译为 NyarVM（所有平台通用）
vcc build --target nyar

# 编译为 WASI Preview 2
vcc build --arch wasm32 --os wasi --abi wasip2
```

## 📌 状态

✅ Web API 绑定已覆盖 10 个模块，包含 60+ API 函数。
🚧 WASI Preview 1/2 专用绑定待补充（文件系统、网络等）。
