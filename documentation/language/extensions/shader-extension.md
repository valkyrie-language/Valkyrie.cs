# Shader 扩展

Valkyrie 语言的 Shader 扩展为 GPU 着色器编程提供领域特定语法，对应 Gnosis 引擎的 gg-shader 语言。

> 🚧 本文档描述设计中的功能，尚未实现。

## 设计理念

游戏引擎需要编写大量着色器代码，传统方式使用 HLSL/GLSL/WGSL 等独立语言。Valkyrie Shader 扩展将着色器编程集成到统一语言中：

| 传统方式 | Valkyrie Shader |
|:---|:---|
| HLSL / GLSL / WGSL | `shader` 一等公民声明 |
| 独立编译管线 | 与 Valkyrie 统一编译管线 |
| 手动 uniform 绑定 | 自动 uniform 推导 |
| 多后端手写 | 编译期多后端生成 |

## Shader 声明

### 顶点着色器

```v
using gg_shader::f32::{vec2, vec3, vec4, mat44}

shader VertexShader {
    input {
        position: vec3;
        normal: vec3;
        uv: vec2;
    }

    output {
        clip_position: vec4;
        world_normal: vec3;
        world_uv: vec2;
    }

    uniform {
        model: mat44;
        view: mat44;
        projection: mat44;
    }

    vertex(input) -> output {
        let world_pos = model * vec4(input.position, 1.0);
        output.clip_position = projection * view * world_pos;
        output.world_normal = (model * vec4(input.normal, 0.0)).xyz;
        output.world_uv = input.uv;
        return output
    }
}
```

### 片段着色器

```v
using gg_shader::f32::{vec2, vec3, vec4, tex2}

shader FragmentShader {
    input {
        world_normal: vec3;
        world_uv: vec2;
    }

    output {
        color: vec4;
    }

    uniform {
        albedo: tex2;
        light_dir: vec3;
    }

    fragment(input) -> output {
        let tex_color = sample(albedo, input.world_uv);
        let lighting = max(dot(normalize(input.world_normal), light_dir), 0.0);
        output.color = vec4(tex_color.rgb * lighting, tex_color.a);
        return output
    }
}
```

### 计算着色器

```v
using gg_shader::f32::{vec3}

shader ParticleCompute {
    input {
        dt: f32;
    }

    uniform {
        particle_buffer: storage_buffer;
        count: u32;
    }

    compute([dim(256, 1, 1)] id: vec3) {
        let index = id.x;
        if index >= count {
            return
        }
        let particle = particle_buffer[index];
        particle.position += particle.velocity * dt;
        particle_buffer[index] = particle
    }
}
```

## 内置类型

Shader 扩展的类型基于 `Vector::<T>` 泛型，通过内置虚拟空间按精度组织。使用 `using` 引入短名，避免泛型书写的麻烦：

```v
using gg_shader::f32::{vec2, vec3, vec4, mat22, mat33, mat44, tex1, tex2, tex3, tex_cube}
using gg_shader::i32::{vec2, vec3, vec4}
using gg_shader::u32::{vec2, vec3, vec4}
```

### 虚拟空间

| 虚拟空间 | 精度 | 可用类型 |
|:---|:---|:---|
| `gg_shader::f32` | 浮点 | `vec2` / `vec3` / `vec4` / `mat22` / `mat33` / `mat44` / `tex1` / `tex2` / `tex3` / `tex_cube` |
| `gg_shader::i32` | 整数 | `vec2` / `vec3` / `vec4` |
| `gg_shader::u32` | 无符号整数 | `vec2` / `vec3` / `vec4` |

### 向量类型

底层为 `Vector::<T, N>` 泛型，通过虚拟空间引入后使用短名：

| 引入名 | 底层类型 | 说明 |
|:---|:---|:---|
| `vec2` | `Vector::<f32, 2>` | 2D 浮点向量 |
| `vec3` | `Vector::<f32, 3>` | 3D 浮点向量 |
| `vec4` | `Vector::<f32, 4>` | 4D 浮点向量 |

从 `gg_shader::i32` 引入的 `vec2` / `vec3` / `vec4` 底层为 `Vector::<i32, N>`，`gg_shader::u32` 同理。

### 矩阵类型

| 引入名 | 底层类型 | 说明 |
|:---|:---|:---|
| `mat22` | `Matrix::<f32, 2, 2>` | 2×2 浮点矩阵 |
| `mat33` | `Matrix::<f32, 3, 3>` | 3×3 浮点矩阵 |
| `mat44` | `Matrix::<f32, 4, 4>` | 4×4 浮点矩阵 |

### 纹理与缓冲区类型

| 引入名 | 底层类型 | 说明 |
|:---|:---|:---|
| `tex1` | `Texture::<f32, 1>` | 1D 采样纹理 |
| `tex2` | `Texture::<f32, 2>` | 2D 采样纹理 |
| `tex3` | `Texture::<f32, 3>` | 3D 采样纹理 |
| `tex_cube` | `TextureCube::<f32>` | 立方体贴图 |
| `storage_buffer` | `StorageBuffer::<T>` | 存储缓冲区 |

### 内置函数

| 函数 | 说明 |
|:---|:---|
| `sample(tex, uv)` | 纹理采样 |
| `normalize(v)` | 向量归一化 |
| `dot(a, b)` | 点积 |
| `cross(a, b)` | 叉积 |
| `reflect(i, n)` | 反射向量 |
| `mix(a, b, t)` | 线性插值 |
| `clamp(x, min, max)` | 限制范围 |
| `max(a, b)` / `min(a, b)` | 最大/最小值 |

## 多后端生成

Valkyrie Shader 编译为多种后端：

| 后端 | 生成目标 | 用途 |
|:---|:---|:---|
| Vulkan / DX12 | SPIR-V | 桌面图形 |
| Metal | MSL | Apple 平台 |
| WebGL / WebGPU | WGSL | Web 平台 |
| OpenGL | GLSL | 兼容平台 |

编译管线：

```
Valkyrie Shader AST
    │
    ▼
IKun IR (Shader Dialect)
    │
    ├──→ SPIR-V Backend
    ├──→ MSL Backend
    ├──→ WGSL Backend
    └──→ GLSL Backend
```

## 与 Gnosis 的映射

| Valkyrie Shader | Gnosis | 说明 |
|:---|:---|:---|
| `shader` 声明 | gg-shader 语言 | Layer 3 着色器 |
| `uniform` | Gnosis.Graphic RHI | 渲染资源绑定 |
| `tex2` | Gnosis.Graphic 纹理 | GPU 纹理资源 |
| `compute` | Gnosis.Compute | 计算管线 |

## 与 NyarVM 的关系

Shader 扩展通过 NyarVM 的 Shader Dialect 实现：

```
Valkyrie Shader AST → AstToIkunConverter → IKun Shader Dialect → 多后端代码生成
```

## 开发状态

| 功能 | 状态 |
|:---|:---|
| shader 声明语法 | 📋 计划中 |
| 内置向量/矩阵类型 | 📋 计划中 |
| 纹理采样 | 📋 计划中 |
| SPIR-V 后端 | 📋 计划中 |
| MSL 后端 | 📋 计划中 |
| WGSL 后端 | 📋 计划中 |
| 计算着色器 | 📋 计划中 |
