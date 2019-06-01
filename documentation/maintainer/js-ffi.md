# JS FFI 体系

Valkyrie 通过属性标注实现与 JavaScript 的类型安全互操作。所有 JS 桥接代码由 `JsBridgeGenerator` 自动生成。

## 两种标注

| 属性 | 含义 | 加载方式 |
|:---|:---|:---|
| `[js_builtin("scope.path")]` | 浏览器内置全局 API | 直接调用，同步 |
| `[js("package", "export")]` | npm 依赖中的导出 | `import()` 动态加载，异步 |

## 关键区别

| | `[js_builtin]` | `[js]` |
|:---|:---|:---|
| 体积 | **零依赖**，不增加 bundle | 增加 import 的库体积 |
| 性能 | 同步调用 | 异步调用 |
| 隔离 | 不序列化 WASM 反射 | 需要 JS bridge 层 |
| 可 DCE | `pure` 标记可被 Dead Code Elimination | 标识为副作用 |
| 适用 | `Math.floor`、`console.log`、`Date.now` | `axios.get`、`lodash.debounce` |

## 胶水代码生成

`JsBridgeGenerator` 按 Valkyrie 函数签名和标注自动生成 JS 胶水代码。胶水实现调用标注中指定的 JS API：

```
Valkyrie 函数签名: micro my_sin(x: f64) -> f64 [js_builtin("Math.sin"), pure]
     ↓
JsBridgeGenerator 生成胶水: export function my_sin(x) { return Math.sin(x) }
```

`pure` 标注表示无副作用，优化器可删除未使用的调用。

## 模块依赖声明

使用 `[js]` 的模块需在 `legion.von` 中声明：

```
[dependencies]
"axios" = "^1.0.0"
"lodash" = "^4.17.0"
```

## API 参考

### ModuleScope（胶水模板中的保留对象）

胶水代码被包装在 ModuleScope 函数中，保证模块隔离：

```javascript
export default async function({ Module, ready }) {
    await ready;
    return {
        greet: async function(name) {
            return Module.greet(name);
        },
        debug: Module.debug  // 纯函数可直接暴露
    };
}
```

### PwaGenerator（Service Worker 生成）

PWA 支持通过 `PwaGenerator` 实现，自动生成 Service Worker：
- 自动缓存所有 WASM 输出
- 自动更新流程
- 离线唤醒与通知
