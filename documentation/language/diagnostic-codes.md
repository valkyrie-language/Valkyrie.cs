# Valkyrie 诊断错误代码目录

> 本文档记录所有 Valkyrie 编译器和类型检查器诊断代码，含修复建议和文档链接。

## 代码结构

错误代码格式：`VALK{0000-9999}`

| 范围 | 类别 |
|:---|:---|
| VALK1001-1999 | 语法解析错误 |
| VALK2001-2999 | 类型检查错误 |
| VALK3001-3999 | 语义分析错误 |
| VALK4001-4999 | IR 生成错误 |
| VALK5001-5999 | 代码生成错误 |
| VALK_Lxxx | Linter 代码质量规则（可配置严重级别） |
| VALK_Exxx | 游戏 ECS 约束规则 |
| VALK_Sxxx | 安全规则 |

## 类型检查错误（VALK2001-2999）

### VALK2001 — 类型不匹配

**消息**：类型不匹配：期望 `{expected}`，实际类型为 `{actual}`

**示例**：
```ggscript
let x: i32 = "hello"  # VALK2001：期望 i32，实际类型为 utf8
```

**建议**：检查变量声明类型与赋值表达式类型是否一致，或使用显式类型转换

### VALK2002 — 未声明的变量

**消息**：未声明的变量 `{name}`

**示例**：
```ggscript
print(x)  # VALK2002：未声明的变量 'x'
```

**建议**：添加变量声明 `let x = ...` 或检查拼写是否正确

### VALK2003 — 重复声明

**消息**：变量 `{name}` 已在此作用域声明

**示例**：
```ggscript
let x = 1
let x = 2  # VALK2003：变量 'x' 已在此作用域声明
```

**建议**：使用不同的变量名，或将第二个声明改为赋值 `x = 2`

### VALK2004 — 函数参数数量不匹配

**消息**：函数 `{name}` 需要 {expected} 个参数，实际提供了 {actual} 个

**示例**：
```ggscript
fn add(a: i32, b: i32): i32 { return a + b }
let sum = add(1)  # VALK2004：函数 'add' 需要 2 个参数，实际提供了 1 个
```

**建议**：补充缺失的参数，或检查函数签名

### VALK2005 — 类型不可转换

**消息**：类型 `{from}` 不可转换为 `{to}`

**示例**：
```ggscript
let x: f64 = "3.14"  # VALK2005：类型 'utf8' 不可转换为 'f64'
```

**建议**：使用内置转换函数如 `parse_f64()` 或 `.to_int()`

### VALK2006 — 未实现 trait

**消息**：类型 `{type}` 未实现 trait `{trait}`

**建议**：为该类型实现所需的 trait，检查 `impl` 块

### VALK2007 — 循环引用

**消息**：类型 `{type}` 存在循环引用

**建议**：使用引用类型或 `Box<>` 打破循环

### VALK2008 — 泛型约束不满足

**消息**：类型 `{type}` 不满足泛型约束 `{constraint}`

**建议**：确保传入的类型满足泛型参数上的 `where` 约束

## Linter 规则（VALK_Lxxx）

| 代码 | 规则 | 级别 | 建议 |
|:---|:---|:---|:---|
| VALK_L001 | 未使用的变量 | Warning | 删除未使用的变量，或使用 `_ = expr` 显式丢弃 |
| VALK_L002 | 未使用的导入 | Warning | 删除未使用的 `import` 语句 |
| VALK_L003 | 过长的函数 | Info | 拆分为多个小函数，每个函数 < 50 行 |
| VALK_L004 | 过深的嵌套 | Warning | 使用提前返回代替深层 if-else 嵌套 |
| VALK_L005 | 重复代码检测 | Info | 提取重复代码为公共函数 |
| VALK_L006 | 可变变量未修改 | Info | 将 `let` 改为 `const` |
| VALK_L007 | 不安全的裸指针 | Warning | 优先使用引用 `&` 代替裸指针 |
| VALK_L008 | 未处理的 result | Warning | 使用 `if let Ok(val) = result` 或 `try!` |
| VALK_L009 | 过大的结构体 | Info | 考虑拆分为嵌套结构体（> 10 字段触发） |

## ECS 规则（VALK_Exxx）

| 代码 | 规则 | 级别 | 建议 |
|:---|:---|:---|:---|
| VALK_E001 | 组件包含逻辑 | Error | 将业务逻辑移到 System 中，Component 只存数据 |
| VALK_E002 | 系统包含状态 | Error | System 应为纯函数，状态存于 Resource 中 |
| VALK_E003 | 查询未使用 | Warning | 删除未使用的 Query 参数 |
| VALK_E004 | System 缺少生命周期 | Info | 添加 setUp/Update/TearDown 生命周期方法 |
| VALK_E005 | 循环依赖 Component | Error | 使用 Entity 引用代替直接引用 Component |

## 安全规则（VALK_Sxxx）

| 代码 | 规则 | 级别 | 建议 |
|:---|:---|:---|:---|
| VALK_S001 | 可变全局状态 | Warning | 将可变状态封装到结构体中，通过方法访问 |
| VALK_S002 | 未检查的可空值 | Error | 使用 `if let Some(val) = opt` 或 `opt!` 操作符 |
| VALK_S003 | 不安全的类型转换 | Warning | 使用 `as?` 安全转换，或实现 `TryFrom<>` trait |
| VALK_S004 | 除零检查缺失 | Warning | 添加 `if divisor != 0` 检查 |
| VALK_S005 | 缓冲区溢出风险 | Error | 使用有界数组访问，或范围检查 `if idx < arr.len()` |

## 使用方式

### 在 CLI 输出中

```
error: VALK2001 (3:10) 类型不匹配：期望 i32，实际类型为 utf8
  💡 建议：检查变量声明类型与赋值表达式类型是否一致
  📖 文档：https://docs.valkyrie.dev/errors/VALK2001
```

### 忽略特定规则

```ggscript
#[allow(VALK_L003)]
fn very_long_function() {
    // ...
}
```
