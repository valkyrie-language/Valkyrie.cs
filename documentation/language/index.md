# 语言参考

Valkyrie 语言是面向 ECS、游戏引擎与 Web 全栈的领域特定语言。参考分为核心语言、领域扩展和规范约定三部分。

## 核心语言

| 文档 | 说明 |
|:---|:---|
| [词法结构](lexical-structure.md) | 关键字、标识符、字面量、运算符、注释 |
| [类型系统](types.md) | 原始类型、复合类型、泛型、可空 |
| [声明](declarations.md) | `micro`、`structure`、`class`、`import` |
| [语句](statements.md) | `if`、`loop`、`while`、`return` |
| [表达式](expressions.md) | 运算符、闭包、管道、成员访问 |
| [模式匹配](pattern-matching.md) | `match`、`case`、`when`、解构 |
| [特性标注](attributes.md) | 元数据标注 |
| [类继承](class-inheritance.md) | 单继承、多继承、具名继承 |
| [复合类型](compound-types.md) | 联合类型、交集类型、函数类型 |
| [容器与可空类型](container-nullable-types.md) | 列表、定长数组、可空后缀 |
| [Effect 系统](effect-system.md) | `catch`、`resume`、代数效应 |
| [路径解析](path-resolution.md) | 名称查找、命名空间、消歧规则 |
| [类型检查](type-checker.md) | 类型系统行为、作用域、诊断 |

## 领域扩展

| 文档 | 说明 |
|:---|:---|
| [ECS 扩展](extensions/ecs-extension.md) | `component`、`system`、`query`、`entity` |
| [Schema 扩展](extensions/schema-extension.md) | `class`、`structure`、`enums`、`flags`、`union`、`storage`、`service` |
| [Async 扩展](extensions/async-extension.md) | `.await`、`.awake`、`.block`、`future` 类型 |
| [Shader 扩展](extensions/shader-extension.md) | GPU 着色器、纹理、计算管线 |
| [Neural 扩展](extensions/neural-extension.md) | 神经网络、张量、推理、训练 |
| [Template 扩展](extensions/template-extension.md) | 元代码块、模板渲染 |
| [AWSL 扩展](extensions/awsl.md) | AWSL UI 声明语言，响应式组件 |

## 规范约定

| 文档 | 说明 |
|:---|:---|
| [语言约定](conventions.md) | 关键字选择、命名规范、设计决策 |
