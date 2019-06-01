# GGScript 模式匹配规范

## 概述

模式匹配（Pattern Matching）是一种强大的语法结构，允许对值进行结构化分解和条件绑定的统一处理。

## 语法设计

### 1. match 表达式

```valkyrie
match <expr> {
    case <pattern>: <result>
    when <term expr>: <result>
    type <type expr>: <result>
    else: <result>
}
```

`match` 表达式对输入表达式进行模式匹配，返回第一个匹配分支的结果。支持四种分支类型：
- `case <pattern>`：模式匹配分支
- `when <term expr>`：守卫分支，条件满足时执行
- `type <type expr>`：类型分支，类型匹配时执行
- `else`：默认分支，等价于 `case _`

### 2. loop 模式匹配

```valkyrie
loop <pattern> in <expr> { <body> }
loop <pattern> in <expr> if <condition> { <body> }
while let <pattern> = <expr> { <body> }
until not <pattern> = <expr> { <body> }
```

`loop` 表达式对可迭代对象的每个元素进行模式匹配并执行循环体。
- `loop <pattern> in <expr>`：遍历表达式结果
- `while let <pattern> = <expr>`：当模式匹配成功时循环
- `until not <pattern> = <expr>`：直到模式不再匹配时停止循环

## 模式类型

### 1. wildcard 模式（通配符）

```valkyrie
match value {
    case _: "matched"
}
```

匹配任意值，不绑定变量。

### 2. 绑定模式（Binding）

```valkyrie
match value {
    case x: "matched: {x}"
}
```

将匹配的值绑定到变量。

### 3. 字面量模式（Literal）

```valkyrie
match value {
    case 42: "the answer"
    case "hello": "greeting"
    case true: "boolean true"
}
```

匹配具体字面量值。

### 4. 枚举模式（Enum）

```valkyrie
match value {
    case Option.Some(x): "got some: {x}"
    case Option.None: "got nothing"
}
```

匹配枚举类型，提取内部值。

### 5. 元组模式（Tuple）

```valkyrie
match value {
    case (x, y): "tuple of {x} and {y}"
    case (a, b, c): "triple of {a}, {b}, {c}"
}
```

匹配元组结构，解构元素。

### 6. 结构模式（Struct）

```valkyrie
match value {
    case Point { x, y }: "{x}, {y}"
    case Rect { origin: Point { x, y }, width, height }: "rect at ({x}, {y})"
}
```

匹配结构体或类，解构字段。

### 7. 列表模式（List）

```valkyrie
match value {
    case []: "empty list"
    case [head, ..tail]: "head: {head}, tail: {tail}"
    case [first, second, ..rest]: "first: {first}, second: {second}"
}
```

匹配列表，支持 head/tail 解构和 rest 模式。

### 8. 引用模式（Reference）

```valkyrie
match value {
    case ref x: "reference to {x}"
    case ref mut x: "mutable reference to {x}"
}
```

匹配引用或可变引用。

### 9. 强制类型模式（Type Cast）

```valkyrie
match value {
    case x as i32: "i32: {x}"
    case x as string: "string: {x}"
}
```

将值强制转换为指定类型后匹配。

## 嵌套模式

模式可以嵌套组合：

```valkyrie
match data {
    case Some([x, y, Point { x: px, y: py }]): "nested: {x}, {y}, {px}, {py}"
    case (Option.Some(value), _, Point { .. }): "complex pattern"
}
```

## 示例

### 示例 1：基本 match

```valkyrie
let result = match maybeValue {
    case Some(x): x + 1
    case None: 0
}

let label = match x {
    case 42: "answer"
    when x > 0: "positive"
    type i32: "integer"
}
```

### 示例 2：loop 遍历列表

```valkyrie
loop (index, item) in enumerate(items) {
    print("[{index}]: {item}")
}

loop [head, ..tail] in list {
    process(head)
    continue with tail
}
```

### 示例 3：解构嵌套结构

```valkyrie
match maybeUser {
    case Some(User { name, age, address: Address { city, country } }):
        "User {name} in {city}, {country}"
    case None: "anonymous"
}
```

### 示例 4：else 默认分支

```valkyrie
match day {
    case "Mon" | "Tue" | "Wed": "early week"
    case "Thu" | "Fri": "late week"
    else: "weekend"
}
```
