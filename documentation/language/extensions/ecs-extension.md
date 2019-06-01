# ECS 扩展

Valkyrie 语言的 ECS 扩展为游戏引擎实体-组件-系统架构提供一等公民语法，直接映射到 Gnosis 引擎的 ECS 子系统。

## 设计理念

传统语言通过框架模拟 ECS 模式，Valkyrie 将 ECS 概念提升为语言原语：

| 传统语言 | Valkyrie |
|:---|:---|
| `class Position : IComponent` | `component Position { ... }` |
| `class MovementSystem : SystemBase` | `system MovementSystem { ... }` |
| `world.Query<Position, Velocity>()` | `query all = Query.all(Position, Velocity)` |
| `entity.GetComponent<Position>()` | `entity.position` |

## Component 声明

组件是纯数据容器，对应 Gnosis ECS 的 Archetype 字段。

### 基础组件

```v
component Position {
    x: f32;
    y: f32;
}

component Velocity {
    dx: f32;
    dy: f32;
}

component Health {
    current: f32;
    max: f32;
}
```

### 带默认值的组件

```v
component Transform {
    x: f32 = 0.0;
    y: f32 = 0.0;
    rotation: f32 = 0.0;
    scale: f32 = 1.0;
}
```

### 带特性标注的组件

```v
[serializable]
[replicated]
component PlayerState {
    health: f32;
    score: i32;
}
```

### 组件约束

- 组件仅包含数据字段，不包含方法
- 字段类型必须为值类型或 `string`
- 不允许嵌套组件引用（使用 Entity 引用代替）

## System 声明

系统是逻辑处理器，包含查询定义和生命周期回调。

### 基础系统

```v
system MovementSystem {
    query all = Query.all(Position, Velocity);

    on_update(frame: Frame) {
        loop entity in query.all {
            entity.position.x += entity.velocity.dx * frame.dt;
            entity.position.y += entity.velocity.dy * frame.dt
        }
    }
}
```

### 查询类型

| 方法 | 说明 | 示例 |
|:---|:---|:---|
| `Query.all(...)` | 包含所有指定组件 | `Query.all(Position, Velocity)` |
| `Query.any(...)` | 包含任一指定组件 | `Query.any(Health, Shield)` |
| `Query.none(...)` | 排除指定组件 | `Query.all(Player).none(AI)` |

查询可链式组合：

```v
query enemies = Query.all(Health, Team).none(Player);
query destructible = Query.all(Health).none(Invulnerable);
```

### 生命周期回调

| 回调 | 签名 | 说明 |
|:---|:---|:---|
| `on_update` | `(frame: Frame)` | 每帧调用 |
| `on_create` | `(entity: Entity)` | 实体创建时调用 |
| `on_destroy` | `(entity: Entity)` | 实体销毁时调用 |
| `on_enable` | `(entity: Entity)` | 组件启用时调用 |
| `on_disable` | `(entity: Entity)` | 组件禁用时调用 |

### 系统执行顺序

```v
[execute_before(PhysicsSystem)]
[execute_after(InputSystem)]
system MovementSystem {
    # ...
}
```

## Entity 操作

### 实体创建

```v
let entity = Entity.create();
entity.add(Position { x: 0.0, y: 0.0 });
entity.add(Velocity { dx: 1.0, dy: 0.0 });
```

### 实体销毁

```v
entity.destroy();
```

### 组件访问

```v
let pos = entity.position;           # 读取组件
entity.position.x += 1.0;           # 修改组件字段
entity.has(Position);                # 检查组件是否存在
entity.remove(Position);             # 移除组件
```

## 与 Gnosis ECS 的映射

| Valkyrie | Gnosis (C#) | 说明 |
|:---|:---|:---|
| `component Position { ... }` | `structure Position : IComponent` | Archetype 字段 |
| `system MovementSystem { ... }` | `class MovementSystem : SystemBase` | System 基类 |
| `Query.all(...)` | `world.Query<...>()` | ECS 查询 |
| `Entity.create()` | `world.CreateEntity()` | 实体创建 |
| `entity.add(...)` | `world.AddComponent(...)` | 组件添加 |
| `on_update(frame)` | `OnUpdate(Frame)` | 帧回调 |



## 开发状态

| 功能 | 状态 |
|:---|:---|
| component 声明 | ✅ 已完成 |
| system 声明 | ✅ 已完成 |
| query 表达式 | ✅ 已完成 |
| Entity 操作 | 🚧 进行中 |
| 生命周期回调 | 🚧 进行中 |
| 执行顺序控制 | 📋 计划中 |
| Archetype 优化 | 📋 计划中 |
