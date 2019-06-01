# 语言约定

Valkyrie 语言的核心关键字约定和设计决策。本文档是所有语言规范的基础参照。

## 关键字约定

Valkyrie 语言的关键字经过精心选择，与常见语言有明确区别：

### 函数 → `micro`

Valkyrie 使用 `micro` 声明函数，不使用 `fn`、`function`、`func`、`def`。

```valkyrie
micro add(a: i32, b: i32) -> i32 {
    return a + b
}

micro greet(name: string) {
    println("Hello, {name}")
}
```

**理由**：`micro` 强调轻量级、可组合的函数语义，与 Valkyrie 的微过程理念一致。在 Schema 扩展中，`micro` 同时作为函数类型注解关键字：`micro(i32) -> i32`。

### 循环 → `loop`

Valkyrie 使用 `loop` 进行遍历和迭代，不使用 `for`、`foreach`。

```valkyrie
loop i in 0..10 {
    println(i)
}

loop item in items {
    process(item)
}

loop pattern in iterator {
    handle(pattern)
}
```

**理由**：`loop` 统一了所有迭代形式——范围遍历、集合遍历、模式匹配遍历。`for` 在游戏引擎中常作为方向向量使用，避免冲突。

### 结构体 → `structure`

Valkyrie 使用 `structure` 声明值类型，不使用 `struct`、`record`。

```valkyrie
structure Point(i32, i32)

structure UserSnapshot {
    id: i32,
    name: string,
    level: i32,
}
```

**理由**：`structure` 明确表达结构相等、不可变、值复制的语义，提供最大程度的可控性。`struct` 在 C# 中有引用语义歧义，`record` 在多语言中含义不一致。大多数情况下使用 `class` 即可，`class` 并不意味着非要装箱，是否装箱取决于优化器的决策。

### 类 → `class`

`class` 用于声明类类型，大多数情况下使用 `class` 即可。`class` 并不意味着非要装箱，是否装箱取决于优化器的决策。`class` 支持继承：

```valkyrie
class Animal
    name: string
    age: i32

class Dog(Animal)
    breed: string
```

## 不存在的关键字

以下关键字在 Valkyrie 中**不存在**，任何文档或代码中不应出现：

| 禁止使用 | 正确替代 | 说明 |
|:---|:---|:---|
| `fn` | `micro` | 函数声明 |
| `function` | `micro` | 函数声明 |
| `func` | `micro` | 函数声明 |
| `def` | `micro` | 函数声明 |
| `for` | `loop` | 遍历循环 |
| `foreach` | `loop` | 集合遍历 |
| `struct` | `structure` | 值类型声明 |
| `record` | `structure` | 值类型声明 |

## 命名规范

### 标识符

| 类别 | 风格 | 示例 |
|:---|:---|:---|
| 类型名（class、structure、enums） | PascalCase | `PlayerState`、`NetworkConfig` |
| 函数名（micro） | snake_case | `calculate_damage`、`process_input` |
| 变量名 | snake_case | `player_health`、`item_count` |
| 常量名 | SCREAMING_SNAKE_CASE | `MAX_HEALTH`、`DEFAULT_PORT` |
| 组件名 | PascalCase | `Position`、`Velocity` |
| 系统名 | PascalCase | `MovementSystem`、`RenderSystem` |
| 枚举变体 | PascalCase | `Active`、`Suspended` |
| 命名空间 | snake_case | `game::physics`、`app::models` |

### 文件扩展名

| 扩展名 | 用途 |
|:---|:---|
| `.v` | Valkyrie 源码 |
| `.script` | Gnosis Script 源码 |
| `.shader` | Gnosis Shader 源码 |
| `.awsl` | AWSL 源码 |
| `.widget` | Gnosis Widget 源码 |
| `.hermes` | Hermes 源码 |
| `.schema` | Gnosis Schema 源码 |
| `.dora` | Dora 模板源码 |
| `.doki` | Doki 模板源码 |

## 设计决策

### 编译器独立设计

VCC 是一个独立的编译器工具，不依赖于任何包管理器。它直接使用 `vendors` 目录中的依赖进行编译。

### ECS 原生语言设计

`component`、`system`、`widget`、`plugin` 是一等公民语法，不是库模拟：

| 传统语言 | Valkyrie |
|:---|:---|
| `class Position : IComponent` | `component Position { ... }` |
| `class MovementSystem : SystemBase` | `system MovementSystem { ... }` |
| `world.Query<Position, Velocity>()` | `query all = Query.all(Position, Velocity)` |

### 前端复用，不自建解析器

Valkyrie 依赖 Oak.Valkyrie 提供词法分析和语法解析，依赖 Nyar.Core 提供中间表示和优化。不自建解析器或优化器，遵循 Nyar 组织的职责分离原则：
- **Oak**：一切文本编解码
- **Acorn**：一切二进制编解码
- **Nyar**：一切分析与优化

### 年度版本号

YearlyVersion（`yearly.major.minor.patch`）消除了 SemVer 预发布标记的歧义：
- `yearly=0`：预研版
- `major=0`：测试版
- `major>0`：稳定版
