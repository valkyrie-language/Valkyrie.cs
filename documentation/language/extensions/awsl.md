# AWSL 扩展

AWSL（Asgard Web Specification Language）是 VOA 框架中的 UI 声明语言，基于 XML 语法的模板系统，扩展了 Valkyrie 以支持声明式 UI 构建。

## 三种标签形式

| 形式 | 语法 | 语义 |
|:---|:---|:---|
| **扩展属性** | `<tag @name/>` | 编译器扩展 `@` 开头属性，编译后删除 |
| **直接标签** | `<tag name/>` | 常规短写 `context.Tag(ContextFn, {})` |
| **二分标签** | `<tag>content</tag>` | 带子内容 `context.Tag(ContextFn, {}, children)` |

## 扩展属性约定

`@` 前缀为编译器扩展标记，编译后直接删除：

```awsl
<card @if={isVisible} @for={item in items}>
  <text>{item.name}</text>
</card>
```

编译后 `@if` 和 `@for` 被转换为条件/循环逻辑，不会出现在最终输出中。

## 脚本与样式子块

二分标签支持 `<script>` 和 `<style>` 子块：

```awsl
<counter-widget>
  <script>
    let count = 0

    micro increment() {
        count += 1
    }
  </script>
  <style>
    .counter {
      font-size: 24px;
      color: #333;
    }
  </style>
  <flex>
    <text class="counter">计数: {count}</text>
    <button @click="increment">+1</button>
  </flex>
</counter-widget>
```

## 响应式系统

AWSL 内建响应式原语，编译为 `voa-runtime.js` 中的对应实现：

| 原语 | 用途 | 运行时对应 |
|:---|:---|:---|
| `Signal` | 可读写状态单元 | `createSignal(initialValue)` |
| `Effect` | 自动追踪依赖的副作用 | `createEffect(fn)` |
| `Memo` | 派生缓存值 | `createMemo(fn)` |

### 自动解包

`<script>` 块中定义的顶层 `let` 变量自动提升为 Signal：

```awsl
<script>
  let count = 0          // → createSignal(0)
  let doubled = count * 2 // → createMemo(() => count() * 2)
</script>
```

### 事件绑定

```awsl
<button @click="handler">点击</button>
<button @click="count += 1">+1</button>
<input @input="name = event.target.value"/>
```

## 指令

| 指令 | 说明 | 示例 |
|:---|:---|:---|
| `@if` | 条件渲染 | `<div @if={isVisible}>` |
| `@for` | 列表渲染 | `<li @for={item in items}>` |
| `@bind` | 双向绑定 | `<input @bind={name}/>` |
| `@on` | 事件绑定简写 | `<div @on:click={handler}>` |
| `@ref` | DOM 引用 | `<canvas @ref={myCanvas}/>` |
| `@class` | 类名合并 | `<div @class:active={isActive}>` |

## 组件系统

### 插槽

```awsl
<!-- 父组件 -->
<card-layout>
  <slot:header>
    <text>标题</text>
  </slot:header>
  <text>默认插槽内容</text>
  <slot:footer>
    <button>确定</button>
  </slot:footer>
</card-layout>

<!-- card-layout 组件定义 -->
<flex>
  <div class="header">{slots.header}</div>
  <div class="body">{slots.default}</div>
  <div class="footer">{slots.footer}</div>
</flex>
```

### 组件导入

```awsl
<import:card-layout from="./components/card-layout.awsl"/>
```

## 编译流程

```
.awsl 文件
  → Oak.Valkyrie Lexer + Parser（AWSL 方言）
  → AwslReactiveCompiler.Compile()
  → JS 输出（响应式组件）
  → CSS 输出（Scoped Style）
```

编译产物分为两部分：JS 文件（包含响应式逻辑和 DOM 操作）和 CSS 文件（自动 Scoped，通过 `data-voa-xxx` 属性隔离）。

## 与 Valkyrie 的关系

AWSL 文件由 VOA 编译器消费，Valkyrie 语言核心不直接识别 AWSL 语法。AWSL 是 VOA 框架层面的领域扩展，通过 ValkyrieRuntime 扩展接口注入。
