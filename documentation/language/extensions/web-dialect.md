# Web 方言

Web 方言（`WebDialect`）是 Nyar Dialect 体系中的 Web 领域方言，封装所有 DOM / 浏览器 API 操作。

## 概念

编译目标为 WASM 时，浏览器 API 不能直接调用。Web 方言为所有浏览器操作提供统一的中转抽象：

```
Valkyrie 源码: create_element("div")
       ↓ AstToIkunConverter
IKunCreateElement 节点
       ↓ WebDialect 降级规则
IKunCall("js", "__voa_create_element", ...)
       ↓ WasmBackend
import { __voa_create_element } from './voa-runtime.js'

运行时: __voa_create_element(tag) → document.createElement(tag)
```

## 领域 IKun 节点

| IKun 节点 | 对应 API |
|:---|:---|
| `IKunCreateElement` | `document.createElement` |
| `IKunSetAttribute` | `element.setAttribute` |
| `IKunAppendChild` | `parent.appendChild` |
| `IKunListener` | `addEventListener` |
| `IKunLocalStorageGet` | `localStorage.getItem` |
| `IKunLocalStorageSet` | `localStorage.setItem` |
| `IKunFetchGet` | `fetch(url)` |
| `IKunTimer` | `setTimeout` / `setInterval` |

## 双标签系统

AWSL 语法支持双标签系统，代码可以出现在三个位置：

```
| 位置 | 语法 | 语义 | 
|:---|:---|:---|
| 扩展属性 | `<tag @name/>` | 编译器扩展 `@` 开头的属性，编译后直接删除 |
| 直接标签 | `<tag name/>` | 常规短写 `context.tag(ContextFn, {})` |
| 二分标签 | `<tag>content</tag>` | 带子内容的块 `context.tag(ContextFn, {}, children)` |
```

二分标签支持 `<script>` 和 `<style>` 子块：

```awsl
<flex @if={true}>
  <script>
    let count = 0
  </script>
  <style>
    .count { font-size: 20px }
  </style>
  <button @click="count++">点击: {count}</button>
</flex>
```

## 编译标记约定

- `@` 前缀为编译器扩展标记，编译后直接删除
- `:` 后缀用于小驼峰到 `-` 符号的编译器扩展，如 `:class` → 类列表合并
