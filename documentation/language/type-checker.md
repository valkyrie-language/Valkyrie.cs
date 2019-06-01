# 类型检查

Valkyrie 的类型检查器在编译期验证程序的类型正确性。

## 类型系统行为

### 15 种类型种类

| TypeKind | 说明 |
|:---|:---|
| `Integer` | 整数类型（i8~i64, u8~u64） |
| `Float` | 浮点类型（f32, f64） |
| `Bool` | 布尔类型 |
| `String` | 字符串类型 |
| `Void` | 空类型 |
| `List` | 列表类型 |
| `Map` | 映射类型 |
| `Nullable` | 可空类型 |
| `Function` | 函数类型 |
| `Class` | 类类型 |
| `Structure` | 结构体类型 |
| `Enum` | 枚举类型 |
| `Union` | 联合类型 |
| `Intersection` | 交集类型 |
| `Error` | 错误类型（类型检查失败时使用） |

### 预定义类型

类型检查器内置以下类型：`i8`~`i64`、`u8`~`u64`、`f32`、`f64`、`bool`、`string`、`void`。

## 作用域管理

三层 Scope 层次：

| 层次 | 说明 | 生命周期 |
|:---|:---|:---|
| 全局 Scope | 顶层声明 | 整个编译过程 |
| 函数 Scope | 函数参数和局部变量 | 函数调用期间 |
| 块 Scope | 块内变量 | 块执行期间 |

### Symbol 定义

每个 Symbol 包含名称、类型和种类：

| SymbolKind | 说明 |
|:---|:---|
| `Variable` | 变量 |
| `Function` | 函数 |
| `Class` | 类 |
| `Structure` | 结构体 |
| `Component` | 组件 |
| `System` | 系统 |
| `Enum` | 枚举 |
| `Flags` | 标志位 |
| `Union` | 联合类型 |
| `Namespace` | 命名空间 |
| `Module` | 模块 |
| `Parameter` | 参数 |

## 类型检查流程

1. **声明检查**：检查所有声明的类型标注是否合法
2. **语句检查**：检查语句中的类型使用是否正确
3. **表达式类型推断**：推断表达式的类型并验证兼容性

## 诊断代码

| 范围 | 说明 |
|:---|:---|
| VALK2001~VALK2010 | 声明相关错误 |
| VALK2011~VALK2020 | 语句相关错误 |
| VALK2021~VALK2029 | 表达式相关错误 |

## 内置函数

| 函数 | 签名 | 说明 |
|:---|:---|:---|
| `print` | `micro(any) -> void` | 输出值（不换行） |
| `println` | `micro(any) -> void` | 输出值（换行） |
