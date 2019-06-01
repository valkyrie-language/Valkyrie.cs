# GGScript 容器与可空类型规范

## 概述

为 GGScript 的类型注解系统添加三种语法糖：
1. **列表类型** `[T]` — 动态长度列表，等价于 `list<T>`
2. **定长数组类型** `[T; N]` — 编译期固定长度数组
3. **可空类型** `T?` — 扁平化的 `T | null`，`T??` 等价于 `T?`

## 语法设计

### 1. 列表类型 `[T]`

```valkyrie
let nums: [i32] = [1, 2, 3]
let names: [string] = []
let nested: [[i32]] = [[1, 2], [3, 4]]
```

等价于 `list<T>`，动态长度，支持 push/pop/迭代。

### 2. 定长数组类型 `[T; N]`

```valkyrie
let vec3: [f32; 3] = [1.0, 2.0, 3.0]
let matrix: [f32; 16] = identity()
let rgb: [u8; 3] = [255, 128, 0]
```

N 必须是编译期常量（整数字面量），长度固定，不可变。

### 3. 可空类型 `T?`

```valkyrie
let name: string? = null
let age: i32? = 42
let items: [i32]? = null
```

**扁平化语义**：
- `T?` 等价于 `T | null`
- `T??` 等价于 `T?`（因为 `(T | null) | null` = `T | null`）
- `T???` 等价于 `T?`
- 这与 `Option<T>` 不同：`Option<Option<T>>` 是嵌套的，`T??` 是扁平的

### 4. 运算符优先级（扩展）

```
? (后缀，最高) → micro → & (交集) → | (联合，最低)
```

`?` 是后缀运算符，绑定最紧：
- `[i32]?` → `([i32])?` → 可空的 i32 列表
- `i32 | f32?` → `i32 | (f32?)` → i32 或可空 f32

## 示例

### 列表

```valkyrie
let nums: [i32] = [1, 2, 3]           # list<i32>
let matrix: [[f32]] = [[1.0], [2.0]]  # list<list<f32>>
```

### 定长数组

```valkyrie
let pos: [f32; 3] = [0.0, 1.0, 2.0]   # array<f32, 3>
let mat: [f32; 16] = identity()         # array<f32, 16>
```

### 可空

```valkyrie
let x: i32? = null                      # i32 | null
let y: i32?? = 42                       # i32 | null（扁平化，等价于 i32?）
let z: [i32]? = null                    # list<i32> | null
let w: string? | i32 = "hello"          # string | null | i32
```

### 对比 Option

```valkyrie
let a: Option<i32> = Some(42)           # Option<i32>，可嵌套
let b: Option<Option<i32>> = Some(Some(42))  # 嵌套合法

let c: i32? = 42                        # i32 | null，扁平
let d: i32?? = 42                       # 等价于 i32?，不是嵌套
```
