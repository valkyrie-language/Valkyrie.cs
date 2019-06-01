# Valkyrie.Formatter

Valkyrie 语言代码格式化器，提供可配置的代码格式化能力。

## 项目结构

```
Valkyrie.Formatter/
├── CodeFormatter.cs              # 格式化核心
├── FormatterConfig.cs            # 格式化配置
└── Valkyrie.Formatter.csproj
```

## 使用方式

```csharp
var formatter = new CodeFormatter();

// 默认配置（4 空格缩进，大括号换行）
var result = formatter.Format(source);

// 紧凑配置（2 空格缩进，声明间无空行）
var compactResult = formatter.Format(source, FormatterConfig.Compact);

// 自定义配置
var config = new FormatterConfig
{
    IndentStyle = IndentStyle.Tab,
    MaxLineWidth = 100,
    BraceStyle = BraceStyle.SameLine
};
var customResult = formatter.Format(source, config);
```

## 配置项

| 属性 | 默认值 | 说明 |
|:---|:---|:---|
| `IndentStyle` | `Space` | 缩进风格（space/tab） |
| `IndentSize` | `4` | 缩进宽度 |
| `MaxLineWidth` | `120` | 最大行宽 |
| `BraceStyle` | `NextLine` | 大括号风格（SameLine/NextLine） |
| `SpaceAroundOperator` | `true` | 运算符两侧空格 |
| `BlankLinesBetweenDeclarations` | `1` | 声明间空行数 |

## 预设方案

- **Default**：4 空格缩进，大括号换行，声明间 1 空行
- **Compact**：2 空格缩进，大括号同行，声明间无空行

## 依赖

| 依赖 | 用途 | 来源 |
|:---|:---|:---|
| `Oak.Valkyrie` | AST 定义 | Oak.cs |

## 详细文档

- [格式化器文档](../../documentation/core-systems/formatter.md)
- [API 参考](../../documentation/development/api-reference.md#valkyrieformatter)
