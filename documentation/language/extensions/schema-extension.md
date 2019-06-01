# Schema 扩展

Valkyrie 语言的 Schema 扩展为数据模式定义提供声明式语法，用于游戏配置、资源描述和网络协议。

> 🚧 本文档描述设计中的功能，部分尚未实现。

## 设计理念

游戏引擎需要大量结构化数据定义（配置文件、资源清单、网络消息），Schema 扩展采用三维正交设计：

| 维度 | 关键字 | 职责 | 生成目标示例 |
|:---|:---|:---|:---|
| **What** | `class`, `structure`, `union`, `enums`, `flags` | 纯数据类型 | TypeScript interface, Go struct |
| **How** | `namespace!` + `model` / `stream` / `cache` | 持久化/缓存/消息队列的形态 | Repository, 序列化器 |
| **Where** | `service` (`http`, `rpc`, `grpc`, `message`, `ws`) | 跨边界通信协议 | HTTP handler, gRPC stub |
| **微过程** | `micro` | 轻量级转换/计算逻辑 | 辅助函数、校验器 |

## 数据类型定义（What）

### class 结构

```v
class User {
    id: i32,
    name: utf8,
    email: utf8,
    status: UserStatus,
    tags: [utf8],
}
```

### structure 结构类型

`structure` 声明不可变值类型，具有结构相等性语义。适用于轻量级数据载体、消息传递、DTO 等场景。

与 `class` 的区别：

| 特性 | `class` | `structure` |
|:---|:---|:---|
| 相等性 | 引用相等 | 结构相等 |
| 可变性 | 字段可变 | 字段不可变 |
| 复制语义 | 引用复制 | 值复制（`with` 表达式） |
| 装箱 | 不一定，取决于优化 | 不涉及 |
| 典型用途 | 实体、状态对象 | 消息、事件、DTO、坐标 |

大多数情况下使用 `class` 即可，`class` 并不意味着非要装箱，是否装箱取决于优化器的决策。`structure` 提供最大程度的可控性。

**位置结构（Positional Structure）**：当字段语义由位置决定时使用紧凑语法。

```v
structure Point(i32, i32)
structure Color(u8, u8, u8, u8)
structure UserId(i32)
```

**命名结构（Named Structure）**：当字段语义由名称决定时使用命名语法。

```v
structure UserSnapshot {
    id: i32,
    name: utf8,
    level: i32,
}
```

**默认值**：命名结构支持字段默认值。

```v
structure NetworkConfig {
    host: utf8 = "localhost",
    port: i32 = 8080,
    timeout: i32 = 30,
}
```

**`with` 表达式**：基于现有记录创建新记录，修改部分字段。

```v
let original = Point(1, 2)
let shifted = original with { x: original.x + 1 }

let config = NetworkConfig()
let production = config with { host: "prod.example.com", port: 443 }
```

**解构**：位置记录支持位置解构，命名记录支持命名解构。

```v
let Point(x, y) = point
let { id, name } = user_snapshot
```

### enums 枚举

```v
enums UserStatus {
    Inactive = 0,
    Active = 1,
    Suspended = 2,
}
```

### flags 标志位

```v
flags Permission {
    Read   = 0b001,
    Write  = 0b010,
    Delete = 0b100,
}
```

### union 联合类型

```v
union StorageBackend {
    Redis,
    Mongo { connection: utf8 },
    LocalFile { path: utf8 },
}
```

**注意**：union 中不带数据的变体直接写 `Name,`，不要写成 `Name()`，否则会被识别为函数。

## 存储声明（How）

使用 `namespace!` 声明主命名空间，通过属性注解绑定存储后端。`model` 直接在命名空间下定义：

**数据库（Database）**：

```v
[database(main="app_main", test="app_test")]
namespace! app;

[index(email, status)]
model UserModel {
    user: &User,
}
```

**缓存（Cache）**：

```v
[cache(main="redis_main")]
namespace! my_redis;

[ttl(7200)]
model SessionCache {
    key: utf8,
    value: User.id,
}
```

**对象存储（Storage）**：

```v
[storage(main="s3_main")]
namespace! my_s3;

model AvatarStorage {
    key: utf8,
    content_type: utf8,
    size_bytes: i64,
}
```

**消息队列（Stream）**：

```v
[stream(main="kafka_main")]
namespace! my_events;

model UserEvent {
    payload: User,
}
```

> ⚠️ 旧的 `storage` 语法块和 `cache`/`stream` 顶层关键字已废弃，请使用 `namespace!` + 属性注解 + 顶层 `model` 声明替代。

## 服务/传输声明（Where）

### service 声明

`service` 声明跨边界通信协议，支持 HTTP REST、gRPC、WebSocket 和消息队列。

```v
service UserService {
    [path("/users/{id}"), json]
    get get_user(id: utf8) -> User

    [path("/users"), json]
    post create_user(user: User) -> User

    grpc watch_user(User.id) -> stream<UserEvent>;
    grpc batch_get([User.id]) -> [User];
    grpc update_user(User) -> User;

    ws user_updates {
        message: User,
    }
}
```

### rpc 声明

`rpc` 在 `service > grpc` 子块中声明远程过程调用端点。

**语义规则**：

| 规则 | 说明 |
|:---|:---|
| 请求类型 | 参数类型必须是 Schema 中已定义的类型，或基础类型 |
| 返回类型 | 支持普通类型和 `stream<T>` 服务端流 |
| 命名规范 | 使用 `snake_case` |
| 通信模式 | 一元调用（请求-响应）和服务端流式推送 |

**通信模式**：

| 模式 | 语法 | 说明 |
|:---|:---|:---|
| 一元调用 | `rpc get_user(User.id) -> User;` | 单请求 → 单响应 |
| 服务端流 | `rpc watch_user(User.id) -> stream<UserEvent>;` | 单请求 → 流式响应 |
| 批量调用 | `rpc batch_get([User.id]) -> [User];` | 列表请求 → 列表响应 |

**与 Gnosis.Network 的映射**：

| rpc 声明 | Gnosis.Network | 说明 |
|:---|:---|:---|
| `rpc` 端点 | `RpcCall` IR 节点 | 编译为 `OpCode.CallNative` |
| 请求序列化 | `Acorn` 二进制编解码 | 使用 Acorn 编码请求/响应 |
| 流式响应 | `stream<T>` → 异步迭代器 | 配合 `.await` 消费 |

**调用方式**：在 `micro` 函数中通过服务引用调用 rpc。

```v
micro fetch_user(id: i32) -> User {
    let user = UserService.get_user(id).await
    return user
}

micro watch_events(id: i32) {
    let events = UserService.watch_user(id).await
    loop event in events {
        process_event(event)
    }
}
```

## Micro 微过程

```v
micro active_adults(users: [User]) -> [User] {
    users.filter(micro(it) { it.age >= 18 && it.status == Active })
}
```

## namespace 与组合

每个 `.her` 文件必须以 `namespace` 声明开头：

```v
namespace app;

using common.*;
using order.Order;

class Order {
    id: i32,
    user_id: i32,
    total: f64,
}
```

## 衍生项目：Hermes

[Hermes](../../Hermes) 项目复用 Valkyrie Schema 扩展语法，独立作为通用 Schema DSL 使用：

| 特性 | Hermes 扩展 |
|:---|:---|
| 三维正交设计 | What/How/Where 分离 |
| 多语言代码生成 | TypeScript, Java, Go, Python |
| 数据库迁移 | Schema diff → 迁移计划 |
| 可视化设计器 | 图形界面编辑 `.her` 文件 |

**依赖关系**：Hermes ← Valkyrie Schema 扩展语法

## 与 Gnosis 的映射

| Valkyrie Schema | Gnosis | 说明 |
|:---|:---|:---|
| `class` | Gnosis.Asset 资源定义 | 游戏资源模式 |
| `structure` | Gnosis.Asset 值类型定义 | 不可变消息/事件/DTO |
| `namespace!` + `model` | Gnosis.Asset 存储映射 | 数据持久化 |
| `service` | Gnosis.Network 服务端点 | 网络通信 |
| `rpc` | Gnosis.Network RPC 端点 | 远程过程调用 |
| `micro` | Gnosis.Script 数据处理 | 轻量级转换 |

## 开发状态

| 功能 | 状态 |
|:---|:---|
| class/enums/flags/union 语法 | ✅ 已实现 |
| structure 结构类型 | ✅ 已实现 |
| `namespace!` + `model`/`stream`/`cache` 语法 | ✅ 已实现 |
| rpc 声明与调用 | 📋 规范已定义，解析器已实现 |
| micro 微过程 | ✅ 已实现 |
| Gnosis 代码生成插件 | 📋 计划中 |
