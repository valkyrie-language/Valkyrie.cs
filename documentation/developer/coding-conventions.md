# 编码规范

## 注释

### 注释语言
- 所有注释使用中文
- XML 文档注释也使用中文

### 注释位置
- 注释放在代码上方，禁止后置注释（行尾注释）
- 建议使用 XML 文档注释

### 结构体、枚举、方法、字段
- 所有 public 成员需要文档注释

## 代码结构

### Region 划分
- 大文件（超过 200 行）必须使用 `#region` 划分代码区域

## 语句规范

### 大括号
- `if`、`for`、`foreach`、`while`、`do`、`switch` 后面必须使用大括号
- 即使只有一行代码，也不能省略
- 大括号独占一行（Allman 风格）

### 空行
- 方法之间保留一个空行
- 逻辑块之间保留一个空行
- 注释上方保留一个空行

### 字符串
- 字符串拼接优先使用字符串插值 `$""`
- 多行字符串使用 `@""`

```csharp
var message = $"用户 {userName} 创建成功";

var sql = @"
    SELECT *
    FROM Users
    WHERE IsActive = 1
";
```

### 异常处理
- 不捕获通用异常 `Exception`，捕获具体异常
- 异常消息使用中文描述

```csharp
try
{
    await SaveUserAsync(user);
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "保存用户失败：{UserId}", user.Id);
    throw;
}
```

## 命名规范

| 元素 | 规范 | 示例 |
|:---|:---|:---|
| 命名空间 | PascalCase | `Valkyrie.Runtime` |
| 类/结构体 | PascalCase | `TypeChecker` |
| 接口 | `I` 前缀 + PascalCase | `IRegistry` |
| 方法 | PascalCase | `CompileToWasm` |
| 属性 | PascalCase | `Diagnostics` |
| 字段 | `_` 前缀 + camelCase | `_logger` |
| 参数 | camelCase | `sourceCode` |
| 局部变量 | camelCase | `result` |
| 常量 | PascalCase | `MaxRetryCount` |
| 枚举成员 | PascalCase | `CompileError` |

## 文件组织

- 每个文件一个主要类型
- 文件路径反映命名空间
- 相关的轻量类型可放在同一文件
