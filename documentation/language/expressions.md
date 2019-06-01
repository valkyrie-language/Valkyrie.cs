# 表达式

## 立即执行与延迟构建

Valkyrie 区分两种求值模式：

| 模式 | 语法 | 行为 |
|:---|:---|:---|
| 立即执行 | `f(x)` | 函数调用，立即求值 |
| 延迟构建 | `x |> f` | 管道算子，构建 IR 容器而非立即执行 |

管道 `|>` 不是 Haskell 的惰性求值，而是延迟构建 IR 节点，在响应式上下文中实现延迟绑定。

## 字面量与标识符

```valkyrie
42              # 整数字面量
3.14            # 浮点字面量
"hello"         # 字符串字面量
true            # 布尔字面量
null            # 空值字面量
my_variable     # 标识符引用
```

## 二元表达式

### 运算符优先级（从低到高）

| 优先级 | 运算符 | 说明 |
|:---|:---|:---|
| 1 | `||` | 逻辑或 |
| 2 | `&&` | 逻辑与 |
| 3 | `==` `!=` `<` `>` `<=` `>=` | 比较 |
| 4 | `|` | 位或 / 联合类型 |
| 5 | `&` | 位与 / 交集类型 |
| 6 | `+` `-` | 加减 |
| 7 | `*` `/` `%` | 乘除取模 |
| 8 | `!` `-`（一元） | 逻辑非、负号 |

```valkyrie
a + b * c       # 等价于 a + (b * c)
a || b && c     # 等价于 a || (b && c)
```

## 赋值表达式

```valkyrie
x = 42
x += 1
x -= 1
x *= 2
```

## 一元表达式

```valkyrie
-x          # 负号
!flag       # 逻辑非
```

## 调用表达式

```valkyrie
println("hello")
add(1, 2)
items.filter(micro(it) { it > 0 })
```

## 索引表达式

Valkyrie 区分两套索引语义：**序数访问** 与 **偏移访问**。

| 语法 | 节点 | 语义 | 例子 |
|:---|:---|:---|:---|
| `a[1]` | OrdinalIndexExpression | 序数访问，`1` 表示第一个元素 | `items[1]` |
| `a⁅0⁆` | OffsetIndexExpression | 偏移访问，`0` 表示第一个元素 | `items⁅0⁆` |
| `a::[0]` | OffsetIndexExpression | `a⁅0⁆` 的等价别名 | `items::[0]` |

### 语义说明

- `a[1]` 与 `a⁅0⁆` 默认不等价，因为它们属于不同的体系。
- 当容器实现了“序数访问”与“偏移访问”两套能力时，二者可以被映射为等价行为。
- 序数访问按人类习惯从 `1` 开始计数。
- 偏移访问按机器习惯从 `0` 开始计数。
- `a::[expr]` 是 `a⁅expr⁆` 的别名，不引入新的 AST 节点。

```valkyrie
items[1] # 第一个元素
items[0] # 运行时错误
items[-1] # 最后一个元素
mapping["key"]
matrix[1, 1]

items⁅0⁆ # 偏移 0 函数
items⁅-1⁆ # 最后一个元素
mapping⁅"key"⁆
matrix⁅0, 0⁆

items::[0]
```

### 容器能力约定

- `class` 通常用于 GC 对象，适合提供稳定引用和可变容器能力。
- `structure` 用于值语义对象，适合轻量数据和局部计算。
- `List` / `Dict` / `Set` 等抽象应通过 `trait` 定义能力接口。
- 具体容器通过 `imply` 提供实现。

### 典型映射

- `List<T>`：支持 `OrdinalIndexExpression` 与 `OffsetIndexExpression`
- `Dict<K,V>`：主要支持 `OrdinalIndexExpression`；若实现暴露内部顺序或偏移能力，则可额外支持 `OffsetIndexExpression`
- `HashMap<K,V>`：通常只支持键访问，不建议暴露偏移访问
- `Set<T>`：通常通过有序容器或迭代器能力间接支持索引访问

## 成员访问表达式

```valkyrie
point.x
entity.position
config.host
```

## 管道表达式

```valkyrie
value |> process |> format
```

管道将左侧值作为第一个参数传入右侧函数。

## 闭包

Valkyrie 提供四层闭包语法，从简到繁：

### 1. 隐式字段 — `.field`

```valkyrie
items.map(.name)
```

极简字段访问，按值捕获，Effect 必须中和。

### 2. 简单 Lambda — `x => expr`

```valkyrie
items.filter(x => x > 0)
```

显式命名参数，按值捕获，Effect 必须中和。

### 3. 复杂 Lambda — `micro(x) -> T { ... }`

```valkyrie
items.map(micro(item) -> string {
    let processed = transform(item)
    return format(processed)
})
```

多语句 + 类型标注，按引用捕获，可传递 Effect。

### 4. 尾随闭包 — `f { $expr }`

```valkyrie
items.map { $1.name }
items.filter { $.active }
items.reduce(0) { $1 + $2 }
```

`$N` 按位置引用，`$.field` 按字段名引用，`$field` 简写形式。可传递 Effect。

## 元代码块

```valkyrie
<% if condition %>
    ...
<% end %>

<% expr %>
```

详见 [Template 扩展](template-extension.md)。