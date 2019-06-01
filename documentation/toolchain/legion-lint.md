# 代码检查器

Valkyrie 代码检查器提供可配置的静态分析规则。

## 设计目标

- 可配置规则集
- 严重级别分类（Error / Warning / Info）
- 自动修复建议
- 与格式化器协作

## 规则分类

### 代码质量

| 代码 | 规则 | 级别 |
|:---|:---|:---|
| VALK_L001 | 未使用的变量 | Warning |
| VALK_L002 | 未使用的导入 | Warning |
| VALK_L003 | 过长的函数 | Info |
| VALK_L004 | 过深的嵌套 | Warning |
| VALK_L005 | 重复代码检测 | Info |

### ECS 规则

| 代码 | 规则 | 级别 |
|:---|:---|:---|
| VALK_E001 | 组件包含逻辑 | Error |
| VALK_E002 | 系统包含状态 | Error |
| VALK_E003 | 查询未使用 | Warning |
| VALK_E004 | 系统缺少生命周期回调 | Info |

### 安全规则

| 代码 | 规则 | 级别 |
|:---|:---|:---|
| VALK_S001 | 可变全局状态 | Warning |
| VALK_S002 | 未检查的可空值 | Error |
| VALK_S003 | 不安全的类型转换 | Warning |

## 配置

```json
{
    "rules": {
        "VALK_L001": "warning",
        "VALK_E001": "error",
        "VALK_S002": "error"
    },
    "exclude": ["tests/"]
}
```

## 使用方式

```bash
vcc check src/                    # 运行检查器
vcc check src/ --fix              # 自动修复
vcc check src/ --rule VALK_L001   # 仅运行指定规则
```
