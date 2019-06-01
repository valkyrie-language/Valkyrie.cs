# GGScript 语言标准 1.0

> 发布日期：2026-05-05
> 版本：1.0.0
> 状态：正式发布

## 概述

GGScript（Great Genesis Script）是 Valkyrie 平台的默认编程语言，设计用于通用计算、游戏脚本和 Web 开发。GGScript 编译到 IKun IR，通过 Nyar Intelligence 优化后生成目标平台代码。

### 设计目标

- **简洁直观**：类 TypeScript 语法，降低学习成本
- **类型安全**：强大的静态类型系统，支持泛型和模式匹配
- **高效执行**：编译到高效字节码或原生代码
- **多平台**：支持 NyarVM、WASM、JVM、CLR 等多个目标平台

## 词法结构

### 标识符

```
identifier ::= identifier_start identifier_part*
identifier_start ::= alpha | '_' | '$'
identifier_part ::= identifier_start | digit
```

### 关键字

```
fn let const if else match loop for while do
return break continue import export from as
struct enum effect async await yield
true false nil
```

### 字面量

```valkyrie
// 整数
42
0xFF
0b1010

// 浮点数
3.14
1e10

// 字符串
"Hello, World!"
"Line: \n"

 // 原始字符串
r#"Raw string with "quotes" and \backslash"#

// 数组
[1, 2, 3]

// 对象
{ name: "test", value: 42 }
```

## 类型系统

### 基本类型

| 类型 | 说明 | 示例 |
|:---|:---|:---|
| `i8` | 8 位有符号整数 | `let x: i8 = 42` |
| `i16` | 16 位有符号整数 | `let x: i16 = 1000` |
| `i32` | 32 位有符号整数 | `let x: i32 = 42` |
| `i64` | 64 位有符号整数 | `let x: i64 = 42` |
| `u8` | 8 位无符号整数 | `let x: u8 = 255` |
| `f32` | 32 位浮点数 | `let x: f32 = 3.14` |
| `f64` | 64 位浮点数 | `let x: f64 = 3.14` |
| `bool` | 布尔值 | `let x: bool = true` |
| `str` | 字符串 | `let x: str = "hello"` |

### 复合类型

```valkyrie
// 数组
let arr: [i32; 3] = [1, 2, 3]

// 元组
let tuple: (i32, str, bool) = (42, "test", true)

// 函数类型
let fn_type: (i32, i32) -> i32
```

### 结构体

```valkyrie
struct Point {
    x: f64,
    y: f64
}

struct Circle {
    center: Point,
    radius: f64
}

let p = Point { x: 1.0, y: 2.0 };
let c = Circle { center: p, radius: 1.5 };
```

### 枚举

```valkyrie
enum Result<T, E> {
    Ok(T),
    Err(E)
}

enum Shape {
    Circle(f64),
    Rectangle(f64, f64),
    Triangle { base: f64, height: f64 }
}
```

### 泛型

```valkyrie
fn identity<T>(x: T) -> T {
    return x;
}

struct Box<T> {
    value: T
}

fn pair<A, B>(a: A, b: B) -> (A, B) {
    return (a, b);
}
```

## 函数

### 函数声明

```valkyrie
fn add(a: i32, b: i32) -> i32 {
    return a + b;
}

// 自动返回
fn multiply(a: i32, b: i32) -> i32 {
    a * b
}

// 多返回值
fn divmod(a: i32, b: i32) -> (i32, i32) {
    (a / b, a % b)
}

// 泛型函数
fn first<T>(list: [T]) -> T {
    list[0]
}
```

### 高阶函数

```valkyrie
fn apply<T, R>(fn: (T) -> R, value: T) -> R {
    fn(value)
}

fn twice(x: i32) -> i32 {
    x * 2
}

let result = apply(twice, 21);  // 42
```

### 闭包

```valkyrie
let add = |x: i32, y: i32| -> i32 { x + y };

let numbers = [1, 2, 3, 4, 5];
let doubled = numbers.map(|n| n * 2);
```

## 表达式

### 算术运算

```valkyrie
let a = 10 + 5;   // 加法
let b = 10 - 3;   // 减法
let c = 4 * 2;    // 乘法
let d = 10 / 3;   // 除法
let e = 10 % 3;   // 取模
let f = 2 ** 4;   // 幂运算
```

### 比较运算

```valkyrie
let a = 10 == 10;  // 等于
let b = 10 != 5;   // 不等于
let c = 10 < 20;    // 小于
let d = 10 > 5;    // 大于
let e = 10 <= 10;  // 小于等于
let f = 10 >= 10;  // 大于等于
```

### 逻辑运算

```valkyrie
let a = true && false;  // 逻辑与
let b = true || false;  // 逻辑或
let c = !true;          // 逻辑非
```

### 控制流

```valkyrie
// 条件表达式
let max = if a > b { a } else { b };

// 循环
let sum = loop {
    count += 1;
    if count >= 10 {
        break count;
    }
};

// For 循环
for i in 0..10 {
    sum += i;
}

// While 循环
while count < 10 {
    count += 1;
}
```

## 模式匹配

### Match 表达式

```valkyrie
match value {
    0 => "zero",
    1 => "one",
    n if n > 0 => "positive",
    _ => "negative"
}

// 枚举匹配
match result {
    Ok(value) => value,
    Err(error) => handle_error(error)
}

// 结构体匹配
match shape {
    Circle(radius) => pi * radius * radius,
    Rectangle(width, height) => width * height,
    Triangle { base, height } => 0.5 * base * height
}
```

### 模式绑定

```valkyrie
match point {
    Point { x: 0, y: 0 } => "origin",
    Point { x, y } => $"x: {x}, y: {y}"
}

let (quotient, remainder) = divmod(10, 3);
```

## Effect 系统

```valkyrie
// 纯函数
fn pure add(a: i32, b: i32) -> i32 {
    a + b
}

// IO 函数
fn io read_file(path: str) -> str {
    // 文件读取
}

// 异步函数
fn async fetch(url: str) -> Response {
    // 网络请求
}

// 函数组合
fn process() -> io async i32 {
    let data = read_file("data.txt");
    let result = await fetch(data);
    return result;
}
```

## 导入与模块

```valkyrie
// 导入标准库
import { map, filter } from "std.list";

// 导入并重命名
import { crypto as crypt } from "std.crypto";

// 导入所有
import "std.io";

// 别名导入
import * as utils from "std";

// 从 URL 导入
import { component } from "./components/button.v";

export fn public_function() { }

export struct PublicStruct { }
```

## 语法速查表

### 变量声明

| 形式 | 作用域 | 可变性 | 示例 |
|:---|:---|:---|:---|
| `let` | 局部 | 不可变 | `let x = 42;` |
| `let mut` | 局部 | 可变 | `let mut x = 42;` |
| `const` | 全局 | 不可变 | `const PI = 3.14;` |

### 类型注解

```valkyrie
let x: i32 = 42;                    // 显式类型
let y = 42;                         // 类型推导
let arr: [i32; 3] = [1, 2, 3];     // 数组类型
let fn_type: (i32) -> i32 = |x| x; // 函数类型
```

### 运算符优先级

| 优先级 | 运算符 | 结合性 |
|:---:|:---|:---:|
| 1 | `**` | 右结合 |
| 2 | `* / %` | 左结合 |
| 3 | `+ -` | 左结合 |
| 4 | `== != < > <= >=` | 左结合 |
| 5 | `&&` | 左结合 |
| 6 | `\|\|` | 左结合 |
| 7 | `=` | 右结合 |

## 标准库模块

| 模块 | 说明 |
|:---|:---|
| `std.io` | 输入输出、文件操作 |
| `std.math` | 数学函数和常量 |
| `std.string` | 字符串操作 |
| `std.list` | 列表和数组操作 |
| `std.map` | 哈希表和映射 |
| `std.set` | 集合操作 |
| `std.option` | Option 类型和操作 |
| `std.result` | Result 类型和操作 |
| `std.sort` | 排序算法 |
| `std.async` | 异步编程 |
| `std.net` | 网络编程 |
| `std.ffi` | 外部函数接口 |
| `std.crypto` | 加密哈希、对称/非对称加密 |
| `std.json` | JSON 解析和序列化 |
| `std.csv` | CSV 数据处理 |
| `std.regex` | 正则表达式 |

## 平台 SDK

| SDK | 目标平台 | 说明 |
|:---|:---|:---|
| `std.adaptor.wasm` | WebAssembly | Web 平台 API |
| `std.adaptor.wasip1` | WASI | WebAssembly 系统接口 |
| `std.adaptor.dotnet` | .NET CLR | .NET 平台 API |
| `std.adaptor.jvm` | JVM | Java 虚拟机 API |
| `std.adaptor.windows` | Windows | Windows 平台 API |
| `std.adaptor.linux` | Linux | Linux 平台 API |
| `std.adaptor.macos` | macOS | macOS 平台 API |

## 版本历史

| 版本 | 日期 | 变更 |
|:---|:---|:---|
| 1.0.0 | 2026-05-05 | 正式发布 |
| 0.9.0 | 2026-04-01 | 泛型、Effect 系统稳定 |
| 0.8.0 | 2026-03-01 | 模式匹配、闭包完善 |
| 0.7.0 | 2026-02-01 | 基础语法和类型系统 |
