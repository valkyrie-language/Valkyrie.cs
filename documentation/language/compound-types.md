# GGScript 复合类型特性规范

## 概述

为 GGScript 的类型注解系统添加三种复合类型：
1. **联合类型** `A | B` — 值可以是 A 或 B 中的任意一种
2. **交集类型** `A & B` — 值必须同时满足 A 和 B
3. **函数类型** `micro(a: A, b: B) -> T` — 函数值的类型签名

## 语法设计

### 1. 联合类型（Union Type）

```valkyrie
let x: i32 | f32 = 42
let name: string | null = "hello"
let result: Success | Error = fetch()
```

语义：值可以是 `|` 分隔的任意一种类型。`|` 是左结合的，`A | B | C` 等价于 `(A | B) | C`。

### 2. 交集类型（Intersection Type）

```valkyrie
let obj: Serializable & Equatable = create()
let widget: Renderable & Clickable & Draggable = build()
```

语义：值必须同时满足 `&` 分隔的所有类型。`&` 是左结合的，`A & B & C` 等价于 `(A & B) & C`。

### 3. 函数类型（Function Type）

```valkyrie
let add: micro(a: i32, b: i32) -> i32 = micro(a, b) { a + b }
let callback: micro(event: Event) -> void = on_click
let transform: micro(i32) -> i32 = micro(x) { x * 2 }
```

语义：`micro` 关键字声明函数类型，括号内为参数列表，`->` 后为返回类型。

简写形式（无参数名）：
```valkyrie
let add: micro(i32, i32) -> i32 = micro(a, b) { a + b }
```

### 4. 运算符优先级

```
&（交集）优先级高于 |（联合）：
A | B & C  →  A | (B & C)

函数类型整体优先级高于联合类型：
micro(i32) -> i32 | string  →  micro(i32) -> (i32 | string)
```

## 类型语义

### 联合类型
- **可赋值性**：T 可赋值给 A | B，当且仅当 T 可赋值给 A 或 B 中的任意一个
- **类型推断**：联合类型的成员类型会根据上下文自动推断

### 交集类型
- **可赋值性**：T 可赋值给 A & B，当且仅当 T 可赋值给 A 且 T 可赋值给 B
- **类型推断**：交集类型的成员类型会根据上下文自动推断

### 函数类型
- **参数协变**：函数类型的参数类型可以是更具体的类型
- **返回值逆变**：函数类型的返回类型可以是更通用的类型
- **可赋值性**：函数类型之间按参数和返回值的兼容性判断

## 示例

### 联合类型

```valkyrie
let value: i32 | f32 | string = 42
let result: Success(i32) | Error(string) = compute()
```

### 交集类型

```valkyrie
let obj: Serializable & Comparable = create()
let handler: EventHandler & Loggable & Configurable = setup()
```

### 函数类型

```valkyrie
let add: micro(a: i32, b: i32) -> i32 = micro(a, b) { a + b }
let map_fn: micro(list<i32>, micro(i32) -> i32) -> list<i32> = transform
let callback: micro() -> void = micro() { println("done") }
```

### 组合使用

```valkyrie
let processor: micro(i32) -> i32 | f32 = choose_handler()
let mixed: (Serializable & Comparable) | null = try_create()
```
