# GGScript 类继承特性规范

## 概述

为 GGScript 的 `class` 声明添加继承能力，支持单继承、多继承、具名继承和属性修饰。

## 语法设计

### 1. 简单继承（隐式命名）

```valkyrie
class ClassName(Base) { }
```

基类字段名自动生成为类型名的 **snake_case** 版本：

| 类型名 | 自动字段名 |
|:---|:---|
| `Base` | `base` |
| `MyComponent` | `my_component` |
| `HTTPServer` | `http_server` |
| `A` | `a` |

**转换规则**：在连续大写字母后遇到小写字母时，在大写字母前插入下划线；所有字母转小写。

### 2. 具名继承（显式命名）

```valkyrie
class ClassName(my_base: Base) { }
```

自定义基类字段名，冒号前为字段名，冒号后为类型名。

### 3. 多继承

```valkyrie
class ClassName(A, B) { }
```

多个基类用逗号分隔，每个基类自动生成 snake_case 字段名。

### 4. 多继承 + 具名（同名类型必须重命名）

```valkyrie
class ClassName(a_base: Base<f32>, b_base: Base<f64>) { }
```

**规则**：同一个类型（只看最外层，泛型参数不看）必须重命名。`Base<f32>` 和 `Base<f64>` 的最外层都是 `Base`，因此必须用具名继承区分。

### 5. 属性 + 修饰词

```valkyrie
class ClassName([attr1] A, [attr2, attr3] B) { }
class ClassName([attr1] my_b: Base) { }
class ClassName([attrs] modifier1 modifier2 A) { }
class ClassName([[attr11(arg11)][attr12] mod12 attr13(arg13)] SomeBase) { }
```

属性放在方括号 `[]` 中，多个属性用逗号分隔；修饰词（如 `public`、`readonly`、`mutable` 等）直接写在基类引用前，两者均可选且可组合。

**嵌套属性示例**：`[[attr11(arg11)][attr12] mod12 attr13(arg13)]` 表示多个属性和修饰词的嵌套组合。

## 继承校验规则

| 规则 | 描述 | 错误码 |
|:---|:---|:---|
| 同类型必须具名 | `class C(Base, Base)` 非法 | VALK3001 |
| 字段名冲突 | 具名继承字段名与自有字段冲突 | VALK3002 |
| 具名冲突 | 多个具名继承使用相同字段名 | VALK3003 |
| 循环继承 | A 继承 B，B 继承 A | VALK3004 |

## 示例

### 示例 1：简单继承

```valkyrie
class Animal
    name: string
    age: i32

class Dog(Animal)
    breed: string
```

等价展开：
```valkyrie
class Dog
    animal: Animal    # 自动生成的基类字段
    breed: string
```

### 示例 2：具名继承

```valkyrie
class Dog(pet: Animal)
    breed: string
```

等价展开：
```valkyrie
class Dog
    pet: Animal       # 自定义基类字段名
    breed: string
```

### 示例 3：多继承

```valkyrie
class Flying
    wingspan: f32

class Swimming
    depth: f32

class Duck(Flying, Swimming)
    quack_volume: f32
```

等价展开：
```valkyrie
class Duck
    flying: Flying      # 自动 snake_case
    swimming: Swimming  # 自动 snake_case
    quack_volume: f32
```

### 示例 4：同名类型必须具名

```valkyrie
class Container(left: Base<f32>, right: Base<f64>)
    label: string
```

### 示例 5：属性修饰

```valkyrie
class MyWidget([serializable] base_view: View, [observable] Model)
    extra: string
```
