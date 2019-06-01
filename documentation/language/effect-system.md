# Effect 系统

Valkyrie 的 Effect 系统基于代数效应（Algebraic Effects），提供结构化的异常处理和副作用捕获能力。

## 核心概念

### Term 与 Effect 的对偶性

| 概念 | 方向 | 关键字 | 说明 |
|:---|:---|:---|:---|
| **Term** | 顺流而下 | `match` | 对值进行模式匹配 |
| **Effect** | 逆流而上 | `catch` | 捕获副作用或异常 |

`match` 从外向内分解值，`catch` 从内向外捕获效应。两者形成对偶。

### 内置变体类型

| 类型 | 变体 | 说明 |
|:---|:---|:---|
| `Fine \| Fail` | `Fine(T)` / `Fail(E)` | 成功或失败 |
| `Some \| None` | `Some(T)` / `None` | 有值或无值 |

## catch 语法

### 前置形式

```valkyrie
catch expr {
    case Fine(value): handle_success(value)
    case Fail(error): handle_error(error)
}
```

### 后置形式

```valkyrie
expr.catch {
    case Fine(value): handle_success(value)
    case Fail(error): handle_error(error)
}
```

### else 分支

```valkyrie
catch expr {
    case Fine(value): handle_success(value)
    else: handle_failure()
}
```

`else` 匹配所有未被前面 `case` 捕获的变体。

## resume 语法

`resume` 在 `catch` 块内恢复被中断的执行流：

```valkyrie
catch perform_read() {
    case Fail(error):
        log(error)
        resume default_value
}
```

`resume` 将值逆流送回 `perform` 发起的位置，执行流从断点继续。

## match 与 catch 的区别

| 特性 | match | catch |
|:---|:---|:---|
| 方向 | 顺流：分解已有值 | 逆流：捕获未处理的效应 |
| 控制流 | 选择分支后继续 | 可用 `resume` 恢复到断点 |
| 返回值 | 分支表达式的值 | 整个 catch 表达式的值 |
| 嵌套 | 逐层解构 | 逆流冒泡直到被捕获 |

## 链式使用

```valkyrie
result
    .catch {
        case Fail(_): resume Fine(0)
    }
    .match {
        case Fine(value): process(value)
    }
```

## 与 Async 的关系

`.await` 本质上是 `perform AsyncWait` 的语法糖：

```valkyrie
# 以下两种写法等价
let result = fetch_data().await
let result = catch perform AsyncWait(fetch_data()) {
    case Fine(value): resume value
}
```

详见 [Async 扩展](async-extension.md)。
