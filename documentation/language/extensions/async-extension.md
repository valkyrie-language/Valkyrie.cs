# Async 扩展

Valkyrie 语言的 Async 扩展为异步操作提供后缀表达式语法，采用 Rust 风格的后缀修饰符而非独立关键字。

> 🚧 本文档描述设计中的功能，部分尚未实现。

## 设计理念

游戏引擎中的异步操作主要有三种消费模式：

| 模式 | 后缀 | 语义 | 类比 |
|:---|:---|:---|:---|
| **等待** | `.await` | 挂起当前协程，直到异步操作完成并返回结果 | Rust `.await` |
| **唤醒** | `.awake` | 触发异步操作后立即返回，不等待结果（即发即弃） | "fire and forget" |
| **阻塞** | `.block` | 阻塞当前线程，直到异步操作完成并返回结果 | `block_on()` |

**为什么选择后缀语法**：

1. **链式调用自然**：`fetch_user(id).await` 比 `await fetch_user(id)` 更符合从左到右的阅读顺序
2. **避免关键字污染**：`await`/`awake`/`block` 不是关键字，不占用标识符空间
3. **类型驱动**：只有 `future<T>` 类型才能使用这些后缀，编译器可精确检查

## future 类型

`future<T>` 表示一个尚未完成的异步计算，最终会产生 `T` 类型的值。

```v
let pending: future<User> = UserService.get_user(42)
```

### future 的创建

| 来源 | 示例 | 说明 |
|:---|:---|:---|
| rpc 调用 | `UserService.get_user(id)` | Schema 扩展中定义的 rpc 端点 |
| 异步函数 | `async_read(path)` | 运行时提供的异步 I/O 函数 |
| 计时器 | `Timer.sleep(1.0)` | 异步延时 |
| 手动构造 | `future.ready(value)` | 立即完成的 future |

### future 的状态

```
future<T>
├── Pending   -- 尚未完成
└── Ready(T)  -- 已完成，携带值
```

## 后缀表达式

### `.await` — 异步等待

挂起当前协程，直到 `future<T>` 完成，返回 `T`。

```v
micro fetch_user(id: i32) -> User {
    let user = UserService.get_user(id).await
    return user
}
```

**类型规则**：

| 表达式 | 类型 |
|:---|:---|
| `expr` | `future<T>` |
| `expr.await` | `T` |

**语义**：

1. 求值 `expr` 得到 `future<T>`
2. 若 future 已完成（Ready），直接返回内部值
3. 若 future 未完成（Pending），挂起当前协程，将控制权交还调度器
4. 当 future 完成后，调度器恢复当前协程，返回结果值

**链式调用**：

```v
micro process(id: i32) -> utf8 {
    let user = UserService.get_user(id).await
    let profile = ProfileService.get_profile(user.id).await
    return profile.name
}
```

### `.awake` — 即发即弃

触发异步操作后立即返回，不等待结果。适用于不需要结果的副作用操作。

```v
micro log_event(event: UserEvent) {
    EventService.emit(event).awake
}
```

**类型规则**：

| 表达式 | 类型 |
|:---|:---|
| `expr` | `future<T>` |
| `expr.awake` | `void` |

**语义**：

1. 求值 `expr` 得到 `future<T>`
2. 将 future 提交到调度器执行
3. 立即返回 `void`，不等待完成
4. 异步操作在后台执行，其结果或错误被静默丢弃

**典型场景**：

| 场景 | 示例 |
|:---|:---|
| 日志上报 | `LogService.track(event).awake` |
| 通知推送 | `PushService.notify(user_id, msg).awake` |
| 缓存预热 | `CacheService.warm(key, value).awake` |
| 遥测数据 | `TelemetryService.record(metric).awake` |

**错误处理**：`.awake` 丢弃异步操作的错误。如需处理错误，使用 `.await` 或 `try` 表达式。

```v
micro safe_log(event: UserEvent) {
    try {
        EventService.emit(event).await
    } catch {
        # 降级处理
    }
}
```

### `.block` — 阻塞等待

阻塞当前线程，直到 `future<T>` 完成。仅允许在同步上下文中使用。

```v
micro load_config() -> Config {
    let config = ConfigService.load("app.cfg").block
    return config
}
```

**类型规则**：

| 表达式 | 类型 |
|:---|:---|
| `expr` | `future<T>` |
| `expr.block` | `T` |

**语义**：

1. 求值 `expr` 得到 `future<T>`
2. 阻塞当前线程，直到 future 完成
3. 返回结果值

**使用限制**：

| 上下文 | `.block` | 说明 |
|:---|:---|:---|
| `system` 生命周期回调 | ❌ 禁止 | 阻塞游戏主线程会导致卡顿 |
| `micro` 函数（非 system） | ✅ 允许 | 同步工具函数 |
| `on_load` / `on_unload` | ⚠️ 谨慎 | 仅用于初始化，不得长时间阻塞 |
| `on_update` | ❌ 禁止 | 每帧调用，阻塞不可接受 |

**编译器检查**：编译器在类型检查阶段验证 `.block` 的使用上下文，在禁止的上下文中使用会产生编译错误。

## 三种模式对比

```v
micro demo() {
    let f: future<User> = UserService.get_user(42)

    let user = f.await       # 挂起协程，等待结果，返回 User
    f.awake                  # 后台执行，立即返回 void
    let user2 = f.block      # 阻塞线程，等待结果，返回 User
}
```

| 特性 | `.await` | `.awake` | `.block` |
|:---|:---|:---|:---|
| 返回类型 | `T` | `void` | `T` |
| 当前协程 | 挂起 | 继续 | 阻塞 |
| 当前线程 | 释放 | 释放 | 阻塞 |
| 结果获取 | ✅ | ❌ | ✅ |
| 错误传播 | ✅ | ❌ | ✅ |
| 适用上下文 | 异步 | 异步 | 同步 |

## 与代数效应的关系

Nyar VM 通过代数效应实现异步操作，`.await`/`.awake`/`.block` 是语法糖：

| 后缀 | 代数效应降级 | 说明 |
|:---|:---|:---|
| `.await` | `perform AsyncWait(handle)` | 挂起协程，注册恢复回调 |
| `.awake` | `perform AsyncSpawn(handle)` | 提交到调度器，不注册回调 |
| `.block` | `perform AsyncBlock(handle)` | 阻塞线程等待完成 |

**JS 后端降级**：

```v
# Valkyrie 源码
let user = UserService.get_user(id).await

# JS 生成代码
const user = await UserService.getUser(id);
```

```v
# Valkyrie 源码
UserService.emit(event).awake

# JS 生成代码
UserService.emit(event);  # fire-and-forget，不 await
```

## 与 Gnosis 的映射

| Valkyrie Async | Gnosis | 说明 |
|:---|:---|:---|
| `future<T>` | Gnosis.Runtime 协程句柄 | 异步计算的运行时表示 |
| `.await` | `CoroutineYield` + `CoroutineResume` | 挂起/恢复协程 |
| `.awake` | `CoroutineSpawn` | 后台启动协程 |
| `.block` | 线程阻塞等待 | 同步上下文专用 |

## 开发状态

| 功能 | 状态 |
|:---|:---|
| `future<T>` 类型 | 📋 规范已定义，待实现 |
| `.await` 后缀表达式 | 📋 规范已定义，待实现 |
| `.awake` 后缀表达式 | 📋 规范已定义，待实现 |
| `.block` 后缀表达式 | 📋 规范已定义，待实现 |
| 协程 IR 节点 | ✅ 已实现（`CoroutineYield`/`CoroutineResume`/`CoroutineSpawn`） |
| 编译器上下文检查 | 📋 计划中 |
