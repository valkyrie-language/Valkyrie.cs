# 声明

## 函数声明 — `micro`

`micro` 是 Valkyrie 中声明函数的唯一关键字。

```valkyrie
micro greet(name: string) {
    println("Hello, {name}")
}

micro add(a: i32, b: i32) -> i32 {
    return a + b
}
```

### 带特性标注

```valkyrie
[export]
micro calculate(x: i32) -> i32 {
    return x * 2
}

[serializable]
micro to_data() -> string {
    return "data"
}
```

### 参数格式

```valkyrie
micro named_params(x: i32, y: i32) -> i32 { x + y }
micro no_params() -> void { println("hello") }
micro with_type(x: i32, name: string) -> void { }
```

## 变量声明

### `let` — 不可变绑定

```valkyrie
let x = 42
let name = "Valkyrie"
let items = [1, 2, 3]
```

赋值后不可修改，适用于派生计算。

### `let mut` — 可变绑定

```valkyrie
let mut counter = 0
counter += 1
```

显式声明可变，适用于可变状态。

### `auto` — 类型推断

```valkyrie
let x: auto = 42           # 推断为 i32
let mut y: auto = "hello"  # 推断为 string
```

## 导入声明

```valkyrie
import std.io
import game::physics
import game::physics::Vector as Vec
import package::module
```

| 形式 | 说明 |
|:---|:---|
| `import std.io` | 标准库导入 |
| `import game::physics` | 作用域路径导入 |
| `import ... as Vec` | 带别名导入 |
| `import package::module` | 外部包导入 |

详见 [路径解析](path-resolution.md)。

## 结构体声明 — `structure`

`structure` 声明不可变值类型，具有结构相等性语义。

### 位置结构

```valkyrie
structure Point(i32, i32)
structure Color(u8, u8, u8, u8)
structure UserId(i32)
```

### 命名结构

```valkyrie
structure UserSnapshot {
    id: i32,
    name: string,
    level: i32,
}
```

### 带默认值

```valkyrie
structure NetworkConfig {
    host: string = "localhost",
    port: i32 = 8080,
    timeout: i32 = 30,
}
```

### `with` 表达式

```valkyrie
let original = Point(1, 2)
let shifted = original with { x: original.x + 1 }

let config = NetworkConfig()
let production = config with { host: "prod.example.com", port: 443 }
```

### 解构

```valkyrie
let Point(x, y) = point
let { id, name } = user_snapshot
```

## 类声明 — `class`

`class` 声明类类型，大多数情况下使用 `class` 即可。`class` 并不意味着非要装箱，是否装箱取决于优化器的决策。`class` 支持继承。

```valkyrie
class Animal
    name: string
    age: i32

class Dog(Animal)
    breed: string
```

详见 [类继承](class-inheritance.md)。

## 组件声明 — `component`

`component` 声明 ECS 纯数据容器。

```valkyrie
component Position {
    x: f32;
    y: f32;
}
```

详见 [ECS 扩展](ecs-extension.md)。

## 系统声明 — `system`

`system` 声明 ECS 逻辑处理器。

```valkyrie
system MovementSystem {
    query all = Query.all(Position, Velocity);

    on_update(frame: Frame) {
        loop entity in query.all {
            entity.position.x += entity.velocity.dx * frame.dt
        }
    }
}
```

详见 [ECS 扩展](ecs-extension.md)。
