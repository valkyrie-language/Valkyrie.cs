# 路径解析

## 路径分隔符

Valkyrie 使用两种路径分隔符，语义不同：

| 分隔符 | 名称 | 语义 |
|:---|:---|:---|
| `::` | 作用域解析 | 编译时名称查找，连接命名空间/类型/模块 |
| `.` | 成员访问 | 运行时值访问，访问字段/方法/属性 |

```valkyrie
game::physics::Vector    # 作用域路径：命名空间 game::physics 中的类型 Vector
entity.position.x        # 成员访问：entity 的 position 的 x
```

## 包限定路径

`package::` 前缀表示包限定路径，由执行环境在编译时注入：

```valkyrie
import package::models::User
```

## 命名空间

### 普通命名空间

```valkyrie
namespace game::physics;
```

### 主命名空间

```valkyrie
namespace! game::core;
```

主命名空间中的声明可以直接通过短名称引用。

### 测试命名空间

```valkyrie
namespace* game::tests;
```

测试命名空间中的声明仅在测试模式下可见。

### 封闭命名空间系统

命名空间是封闭的——同一命名空间可以在多个文件中声明，编译器自动合并。

## 四种环境规则

| 环境 | 说明 |
|:---|:---|
| **Namespace** | 命名空间声明和引用 |
| **Type** | 类型名称查找 |
| **Term** | 值和函数名称查找 |
| **REPL** | REPL 交互环境，宽松查找 |

## 必须使用 `::` 的场景

1. 命名空间路径：`game::physics`
2. 包限定导入：`package::module`
3. 嵌套类型引用：`Option::Some`
4. 跨命名空间引用：`std::io::println`
5. 别名导入：`import game::physics::Vector as Vec`

## 解析优先级

名称查找按以下优先级进行：

1. 当前作用域的局部变量
2. 当前命名空间的声明
3. 父命名空间的声明
4. `import` 导入的名称
5. 全局内置名称

如果名称冲突，使用 `::` 完整路径消歧。
