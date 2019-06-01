# Valkyrie.TypeChecker

Valkyrie 语言类型检查器，提供类型推断、作用域管理和诊断输出。

## 项目结构

```
Valkyrie.TypeChecker/
├── TypeChecker.cs                # 类型检查核心
├── TypeDiagnostic.cs             # 诊断输出
├── Scope/
│   ├── Scope.cs                  # 作用域实现
│   └── Symbol.cs                 # 符号定义
├── TypeSystem/
│   └── ValkyrieType.cs           # 类型系统
└── Valkyrie.TypeChecker.csproj
```

## 类型系统

### 原始类型

`i8`~`i64`、`u8`~`u64`、`f32`、`f64`、`bool`、`string`、`void`、`auto`

### ECS 类型

`Component`、`System`、`Widget`、`Plugin`

### 泛型类型

`list<T>`、`map<K,V>`、`nullable<T>`

### 类型兼容性

`IsAssignableFrom` 支持：相同类型、Nullable 兼容、泛型兼容、隐式数值转换、Error 兼容

## 作用域管理

嵌套作用域链：全局作用域 → 函数作用域 → 块作用域

- `Define(symbol)` — 定义符号，不允许重复
- `Resolve(name)` — 沿父作用域链查找
- `ResolveLocal(name)` — 仅当前作用域查找

## 诊断代码

`VALK2001`~`VALK2029`，覆盖变量错误、类型不匹配、声明和作用域错误

## 依赖

| 依赖 | 用途 | 来源 |
|:---|:---|:---|
| `Oak.Valkyrie` | AST 定义 | Oak.cs |

## 详细文档

- [类型检查器文档](../../documentation/core-systems/type-checker.md)
- [API 参考](../../documentation/development/api-reference.md#valkyrietypechecker)
