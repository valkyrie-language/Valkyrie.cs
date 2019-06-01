# 标准库架构

Valkyrie 标准库采用**按平台分发**策略，在编译时按目标平台跨后端映射为原生函数。

## 按平台分发

不同后端使用不同的底层运行时：

| 后端 | 运行时 | 标准库来源 |
|:---|:---|:---|
| NyarVM | NyarVM Executor | Nyar 指令实现 |
| WASM | 浏览器运行时 | voa-runtime.js |
| JVM | JRE | .NET 到 Java API 映射 |
| CLR | .NET BCL | 直接使用 .NET API |
| Native | 系统 API | POSIX / Win32 系统调用 |

## 当前标准库模块

| 模块 | 功能 |
|:---|:---|
| `std.app` | ECS 应用框架 |
| `std.console` | 控制台输出 |
| `std.crypto` | 哈希和加密 |
| `std.dom` | DOM 操作（Web 方言） |
| `std.fetch` | HTTP 客户端（Web 方言） |
| `std.json` | JSON 序列化 |
| `std.math` | 数学函数 |
| `std.storage` | 本地存储（Web 方言） |
| `std.timer` | 定时器 |
| `std.types` | 内置类型操作 |
| `std.url` | URL 解析 |

## 客户端与标准库分支

标准库根据目标的 Web 能力分为两个分支：

| 分支 | 模块 | 目标 |
|:---|:---|:---|
| **WASM** | `std.dom`、`std.storage`、`std.fetch`、`std.timer` | 浏览器环境 |
| **Native** | `std.console`、`std.math`、`std.json`、`std.crypto`、`std.url` | 原生执行 |

编译目标为 `wasm` 时，`std.dom`、`std.storage`、`std.fetch` 可用；目标为 `native` 时，这些模块排除。

## 实现方式

**Oak.Valkyrie 不识别标准库语义。** 所有标准库函数视为普通未解析调用，由 `BaseConverter` 按目标后端转换为特定形式：

```csharp
// "std.dom.create_element" ↦ 创建 IKunCreateElement 节点
var node = new IKunCreateElement(tagName, parentHandle);
egraph.AddLeaf(node);
```

## WASM FFI 映射

VOA 中声明的 FFI 函数自动映射到标准库：

| FFI 属性 | 映射到标准库 |
|:---|:---|
| `[js_builtin("document.createElement")]` | `std.dom.create_element` |
| `[js_builtin("document.querySelector")]` | `std.dom.query_selector` |
| `[js_builtin("addEventListener")]` | `std.dom.add_event_listener` |
