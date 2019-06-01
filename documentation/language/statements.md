# 语句

## 块语句

```valkyrie
{
    let x = 1
    let y = 2
    x + y
}
```

块语句用大括号 `{}` 包裹，最后一个表达式的值作为块的返回值。

## 条件语句

### if

```valkyrie
if x > 0 {
    println("positive")
}
```

### if-else

```valkyrie
if x > 0 {
    println("positive")
} else {
    println("non-positive")
}
```

### if-else if-else

```valkyrie
if x > 0 {
    println("positive")
} else if x < 0 {
    println("negative")
} else {
    println("zero")
}
```

## 循环语句

### while — 条件循环

```valkyrie
let mut count = 0
while count < 10 {
    count += 1
}
```

### loop — 遍历循环

`loop` 是 Valkyrie 的统一迭代语法，替代 `for` 和 `foreach`。

#### 范围遍历

```valkyrie
loop i in 0..10 {
    println(i)
}
```

#### 集合遍历

```valkyrie
loop item in items {
    process(item)
}
```

#### 带条件过滤

```valkyrie
loop item in items if item.active {
    process(item)
}
```

#### 模式匹配遍历

```valkyrie
loop (index, item) in enumerate(items) {
    println("[{index}]: {item}")
}

loop [head, ..tail] in list {
    process(head)
}
```

#### while let — 条件模式匹配

```valkyrie
while let Some(value) = try_next() {
    process(value)
}
```

## 返回语句

```valkyrie
micro add(a: i32, b: i32) -> i32 {
    return a + b
}

micro early_return(x: i32) -> i32 {
    if x < 0 {
        return 0
    }
    return x * 2
}
```

## 表达式语句

任何表达式都可以作为语句使用：

```valkyrie
println("hello")
counter += 1
x + y
```
