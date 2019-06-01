# 代码格式化器

Valkyrie 代码格式化器自动统一代码风格。

## 配置选项

| 选项 | 类型 | 默认值 | 说明 |
|:---|:---|:---|:---|
| `indent_style` | `space` / `tab` | `space` | 缩进风格 |
| `indent_size` | int | 4 | 缩进宽度 |
| `line_width` | int | 120 | 最大行宽 |
| `brace_style` | `same_line` / `next_line` | `next_line` | 大括号风格 |
| `space_before_paren` | bool | false | 函数名后空格 |
| `space_around_operator` | bool | true | 运算符周围空格 |
| `space_after_comma` | bool | true | 逗号后空格 |
| `trailing_comma` | bool | true | 尾随逗号 |
| `semicolon` | bool | false | 语句末尾分号 |
| `blank_line_after_declaration` | bool | true | 声明后空行 |
| `align_attributes` | bool | true | 对齐特性标注 |
| `wrap_long_lines` | bool | true | 折叠长行 |

## 预设方案

### Default

```json
{
    "indent_style": "space",
    "indent_size": 4,
    "brace_style": "next_line",
    "line_width": 120
}
```

### Compact

```json
{
    "indent_style": "space",
    "indent_size": 2,
    "brace_style": "same_line",
    "line_width": 100
}
```

## 格式化范围

| 类别 | 规则 |
|:---|:---|
| 声明 | micro、structure、class、component、system 的缩进和大括号 |
| 语句 | if、loop、while 的缩进和大括号 |
| 表达式 | 二元运算符换行、闭包格式、管道换行 |
| 特性标注 | 独立行、参数对齐 |
| 类型注解 | 联合类型换行、函数类型参数对齐 |

## 使用方式

```bash
vcc format src/               # 格式化目录
vcc format src/ --check       # 仅检查，不修改
vcc format src/ --preset compact  # 使用预设
```
