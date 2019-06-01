# Neural 扩展

Valkyrie 语言的 Neural 扩展为神经网络定义、训练和推理提供领域特定语法，直接操作底层算子，对应 Gnosis 引擎的 AI 子系统和 NyarVM 的 Tensor Dialect。

> 🚧 本文档描述设计中的功能，尚未实现。

## 设计理念

游戏 AI 越来越依赖神经网络（行为树增强、NPC 决策、程序化生成、大模型对话），Valkyrie Neural 扩展将神经网络定义集成到游戏逻辑中，基于自研 Tensor IR 直接操作底层算子：

| 传统方式 | Valkyrie Neural |
|:---|:---|
| Python 训练 + 导出部署 | `neural` 一等公民声明 |
| 运行时加载模型文件 | 编译期网络定义 |
| 第三方推理引擎调度 | NyarVM 直接调度底层算子 |
| CPU/GPU 手动调度 | NyarVM 自动调度 |
| Python 微调 + 导出部署 | 多阶段编程：同一语言训练与推理 |
| 外部 LLM API 调用 | `neural` 声明 + 编译期图优化 |

### 多阶段编程加速

Valkyrie Neural 的核心优势在于利用多阶段编程能力，在编译期完成尽可能多的工作，运行时只执行最优代码：

| 阶段 | 编译期（Stage 0） | 运行时（Stage 1） |
|:---|:---|:---|
| 网络结构 | `neural` 声明 → IKun EGraph | — |
| 算子优化 | EGraph 饱和 + 算子融合 + 常量折叠 | — |
| 权重特化 | 部分求值：已知权重 → 特化内核 | — |
| 调度决策 | 成本模型驱动 → 最优调度 | — |
| 代码生成 | IKunTree → CUDA/CPU/Metal 内核 | — |
| 推理执行 | — | 直接执行特化内核 |

```
┌──────────────────────────────────────────────────────────────────────────┐
│                     Stage 0：编译期（开发时）                              │
│                                                                          │
│  neural 声明 → AST → IKun EGraph → 饱和优化 → 成本提取 → 特化内核生成      │
│                                                                          │
│  · 算子融合：Conv+BN+Relu → FusedConvBnRelu                              │
│  · 注意力融合：QKV+Softmax+Dropout → FusedAttention                      │
│  · 常量折叠：静态权重注入内核                                              │
│  · 部分求值：固定 batch_size → 循环展开 → 无分支代码                        │
│  · 调度优化：成本模型选择 CPU/GPU 路径                                      │
│  · KV-Cache 特化：推理模式预分配静态图                                      │
└──────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                     Stage 1：运行时（游戏时）                              │
│                                                                          │
│  直接执行编译期生成的特化内核，零开销调度                                    │
│                                                                          │
│  · 无模型加载延迟                                                         │
│  · 无运行时图构建                                                         │
│  · 无动态调度开销                                                         │
│  · 无解释器分发                                                           │
└──────────────────────────────────────────────────────────────────────────┘
```

## Neural 声明

### 前馈网络

```v
neural EnemyDecisionNet {
    input {
        player_distance: f32;
        health_ratio: f32;
        ammo_count: i32;
        alert_level: f32;
    }

    output {
        attack: f32;
        flee: f32;
        patrol: f32;
        hide: f32;
    }

    layers {
        dense(64, activation: relu)
        dense(32, activation: relu)
        dense(4, activation: softmax)
    }
}
```

### 卷积网络

```v
neural TerrainClassifier {
    input {
        heightmap: Dense(ArrayND<f32, [u32; 3]>);
    }

    output {
        terrain_type: Dense(ArrayND<f32, [u32; 1]>);
    }

    layers {
        conv2d(filters: 32, kernel: [3, 3], activation: relu)
        max_pool2d(pool_size: [2, 2])
        conv2d(filters: 64, kernel: [3, 3], activation: relu)
        max_pool2d(pool_size: [2, 2])
        flatten()
        dense(128, activation: relu)
        dense(10, activation: softmax)
    }
}
```

### 循环网络

```v
neural DialogueGenerator {
    input {
        token_ids: Dense(ArrayND<i32, [u32; 1]>);
    }

    output {
        next_token: Dense(ArrayND<f32, [u32; 1]>);
    }

    layers {
        embedding(vocab_size: 10000, dim: 128)
        lstm(units: 256, return_sequences: true)
        lstm(units: 256)
        dense(vocab_size, activation: softmax)
    }
}
```

### Transformer 网络

```v
neural NPCDialogueModel {
    input {
        token_ids: Dense(ArrayND<i32, [u32; 1]>);
        attention_mask: Dense(ArrayND<i32, [u32; 1]>);
    }

    output {
        logits: Dense(ArrayND<f32, [u32; 1]>);
    }

    layers {
        embedding(vocab_size: 32000, dim: 512)
        position_encoding(max_len: 2048, dim: 512)
        transformer_block(heads: 8, dim: 512, ffn_dim: 2048, activation: gelu)
        transformer_block(heads: 8, dim: 512, ffn_dim: 2048, activation: gelu)
        transformer_block(heads: 8, dim: 512, ffn_dim: 2048, activation: gelu)
        layer_norm()
        dense(vocab_size)
    }
}
```

### 大语言模型（LLM）

```v
neural StoryTellerLLM {
    input {
        token_ids: Dense(ArrayND<i32, [u32; 1]>);
        position_ids: Dense(ArrayND<i32, [u32; 1]>);
    }

    output {
        logits: Dense(ArrayND<f32, [u32; 2]>);
    }

    layers {
        embedding(vocab_size: 32000, dim: 1024)
        rms_norm()
        rotary_position_encoding(dim: 1024, base: 10000)

        # 编译期展开：repeater 在 EGraph 中复制 N 份 transformer_block
        # 每份独立优化，支持不同层的算子融合策略
        repeater(24) {
            transformer_block(heads: 16, dim: 1024, ffn_dim: 4096, activation: silu)
        }

        rms_norm()
        dense(vocab_size, bias: false)
    }

    # 推理模式配置
    inference {
        kv_cache: enabled
        kv_cache_dtype: f16
        max_batch_size: 8
        max_seq_len: 4096
    }
}
```

## 推理

### 加载预训练权重

```v
let model = EnemyDecisionNet.load("models/enemy_decision.weights");
```

### 执行推理

```v
let output = model.infer(
    player_distance = 10.0,
    health_ratio = 0.8,
    ammo_count = 5,
    alert_level = 0.3
);

if output.attack > 0.7 {
    enemy.attack(player)
} else if output.flee > 0.5 {
    enemy.flee()
}
```

### 批量推理

```v
let batch_input = Dense(ArrayND<f32, [u32; 2]>::from([
    [10.0, 0.8, 5, 0.3],
    [2.0, 0.3, 0, 0.9],
    [50.0, 1.0, 30, 0.1]
]));

let batch_output = model.infer_batch(batch_input);
```

### LLM 自回归生成

```v
let model = StoryTellerLLM.load("models/storyteller.weights");

# 首次推理：prefill 阶段，处理完整 prompt
let prompt_tokens = tokenizer.encode("勇者走进了黑暗的森林，");
let prefilled = model.prefill(token_ids: prompt_tokens);

# 自回归生成：decode 阶段，逐 token 生成
let mut tokens = prompt_tokens;
loop i in 0..256 {
    let logits = model.decode_next(prefilled.kv_cache);
    let next_token = tokenizer.sample(logits, temperature: 0.8, top_p: 0.95);
    if next_token == tokenizer.eos {
        break
    }
    tokens.push(next_token)
}

let story = tokenizer.decode(tokens);
```

### 流式生成

```v
let model = StoryTellerLLM.load("models/storyteller.weights");

# stream_generate 返回异步迭代器，配合 .await 逐 token 消费
let stream = model.stream_generate(
    prompt: "勇者走进了黑暗的森林，",
    max_tokens: 256,
    temperature: 0.8,
    top_p: 0.95
);

loop token in stream.await {
    ui.show_text(tokenizer.decode([token]))
}
```

### KV-Cache 管理

```v
let model = StoryTellerLLM.load("models/storyteller.weights");

# 手动管理 KV-Cache，支持多轮对话
let mut cache = model.create_kv_cache();

# 第一轮对话
let response_1 = model.chat("你好，请介绍一下自己", kv_cache: cache);

# 第二轮对话复用 KV-Cache，避免重复计算
let response_2 = model.chat("你能讲一个故事吗？", kv_cache: cache);

# 清理缓存
cache.release()
```

## 训练

### 基础训练

```v
neural SimplePredictor {
    input { x: f32; }
    output { y: f32; }

    layers {
        dense(16, activation: relu)
        dense(1)
    }

    training {
        optimizer: adam(learning_rate = 0.001)
        loss: mse
        epochs: 100
        batch_size: 32
    }
}
```

### LLM 微调（LoRA）

```v
neural StoryTellerLLM {
    # ... 同上声明 ...

    training {
        # 冻结基座模型权重，仅训练 LoRA 适配器
        mode: lora
        lora_rank: 16
        lora_alpha: 32
        lora_target: [query, value, ffn_up, ffn_down]

        optimizer: adamw(learning_rate = 0.0002, weight_decay = 0.01)
        loss: cross_entropy
        epochs: 3
        batch_size: 4

        # 学习率调度
        scheduler: cosine_warmup(warmup_steps: 100)

        # 梯度累积（等效 batch_size = 4 * 8 = 32）
        gradient_accumulation: 8

        # 混合精度训练
        precision: bf16

        # 梯度检查点：用重计算换显存
        checkpoint: [transformer_block.6, transformer_block.12, transformer_block.18]
    }
}
```

### 训练执行

```v
# 加载训练数据
let dataset = Dataset.load("data/dialogues.jsonl")
    .shuffle(seed: 42)
    .map(micro(row) { tokenizer.encode(row.text) })
    .batch(4);

# 加载基座模型并启动训练
let base_model = StoryTellerLLM.load("models/storyteller.weights");
let trainer = base_model.train(dataset);

# 训练循环
loop epoch in 0..3 {
    let mut total_loss = 0.0;
    loop batch in dataset {
        let loss = trainer.step(batch).await;
        total_loss += loss;
    }
    print("Epoch {epoch}: loss = {total_loss / dataset.len()}");

    # 保存检查点
    trainer.save_checkpoint("checkpoints/epoch_{epoch}.ckpt");
}

# 导出 LoRA 适配器（仅包含增量权重，体积小）
trainer.export_lora("models/storyteller_lora.weights");
```

### 多阶段训练与推理衔接

Valkyrie 的多阶段编程让训练和推理在同一语言中无缝衔接，编译器自动处理模式切换：

```v
# 编译期：neural 声明生成训练图和推理图两个版本
# 训练图：包含 Grad + OptimizerStep + Loss 节点
# 推理图：仅包含前向传播，额外应用推理特化优化

# 运行时：根据使用方式自动选择
let model = StoryTellerLLM.load("models/storyteller.weights");

# .train() 选择训练图，包含反向传播
let trainer = model.train(dataset);

# .infer() / .prefill() / .decode_next() 选择推理图，应用 KV-Cache 特化
let output = model.infer(token_ids: prompt);
```

编译管线自动生成两套内核：

```
neural StoryTellerLLM 声明
    │
    ├──→ 训练图（含 Grad/OptimizerStep/Loss/BatchNormTraining/Dropout）
    │       │
    │       ▼ EGraph 饱和优化
    │       · 梯度融合
    │       · Adam 更新特化（静态超参 → 无循环代码）
    │       · 重计算策略选择（Checkpoint vs Materialize）
    │       │
    │       ▼ 代码生成
    │       → CUDA 训练内核（bf16 + 梯度累积 + 检查点）
    │
    └──→ 推理图（仅前向传播，无 Dropout，BatchNorm → 推理模式）
            │
            ▼ EGraph 饱和优化
            · Conv+BN+Relu → FusedConvBnRelu
            · QKV+Softmax+Dropout → FusedAttention
            · 部分求值：静态权重 → 特化内核
            · KV-Cache 图特化
            · 连续转置消除
            │
            ▼ 代码生成
            → CUDA 推理内核（f16 KV-Cache + Flash Attention）
            → CPU 推理内核（量化 + SIMD）
```

## Array 类型

Neural 扩展使用 `ArrayND<T, [u32; N]>` 统一表示多维数组，layout 参数 `[u32; N]` 描述内存布局的维度数，与普通 `Array<T>` 是不同的概念。遵循 Valkyrie 惰性约定：大写表示不计算（延迟求值），`Dense(...)` 包装表示密集存储的具体表示。

### Layout 与普通 Array 的区别

`ArrayND<T, [u32; N]>` 的 `[u32; N]` 是 layout 参数，描述的是内存布局的维度数（rank），而不是普通 `Array<T>` 的元素个数。即便是 1D 的情况，`ArrayND<T, [u32; 1]>` 也与 `Array<T>` 本质不同——前者是带 layout 语义的多维数组，后者是普通的一维容器。

```v
# 普通 Array：元素容器，长度是运行时值
let arr: Array<f32> = [1.0, 2.0, 3.0];

# ArrayND：带 layout 的多维数组，[u32; N] 是 layout 维度数
let tensor: Dense(ArrayND<f32, [u32; 1]>) = ArrayND::from([1.0, 2.0, 3.0]);
let matrix: Dense(ArrayND<f32, [u32; 2]>) = ArrayND::from([[1.0, 2.0], [3.0, 4.0]]);
```

### 类型体系

| 类型 | 说明 | 示例 |
|:---|:---|:---|
| `Dense(ArrayND<f32, [u32; 1]>)` | 1D 浮点密集数组 | 分类概率、Token 序列 |
| `Dense(ArrayND<f32, [u32; 2]>)` | 2D 浮点密集数组 | 隐藏状态、序列特征 |
| `Dense(ArrayND<f32, [u32; 3]>)` | 3D 浮点密集数组 | 高度图、单通道特征图 |
| `Dense(ArrayND<f32, [u32; 4]>)` | 4D 浮点密集数组 | 图像批次 [N,C,H,W] |
| `Dense(ArrayND<f32, [u32; N]>)` | N 维浮点密集数组 | Q/K/V 注意力 |
| `Dense(ArrayND<i32, [u32; 1]>)` | 1D 整数密集数组 | Token ID 序列 |
| `Dense(ArrayND<f16, [u32; 2]>)` | 半精度密集数组 | KV-Cache |

### 惰性约定

Valkyrie 惰性约定：大写 = 延迟求值（不立即计算），小写 = 立即求值。

```v
# Dense 大写 → 延迟求值，构建计算图，运行时按需计算
let x: Dense(ArrayND<f32, [u32; 2]>) = matmul(a, b);

# dense 小写 → 立即求值，直接执行计算
let y: dense(ArrayND<f32, [u32; 2]>) = matmul(a, b);
```

在 `neural` 声明中，输入输出默认使用 `Dense(...)` 延迟求值，编译期优化后再生成特化内核。

### Array 操作

```v
let a = Dense(ArrayND<f32, [u32; 2]>::zeros());
let b = Dense(ArrayND<f32, [u32; 2]>::ones());
let c = a + b;
let d = matmul(a, b);
let e = reshape(d, [9]);
let f = softmax(e);
```

### 高级 Array 操作

```v
# 注意力计算
let q = matmul(hidden, wq);
let k = matmul(hidden, wk);
let v = matmul(hidden, wv);
let scores = matmul(q, transpose(k, [0, 2, 1])) / sqrt(head_dim);
let weights = softmax(scores);
let context = matmul(weights, v);

# 位置编码
let pe = rotary_position_encoding(hidden, position_ids, dim: 1024, base: 10000);

# RMS 归一化
let normalized = rms_norm(hidden, weight, epsilon: 1e-6);

# SwiGLU 激活（LLM FFN 常用）
let gate = matmul(hidden, w_gate);
let up = matmul(hidden, w_up);
let activated = silu(gate) * up;
let output = matmul(activated, w_down);
```

## 底层算子

Neural 扩展基于自研 Tensor IR，直接映射到 NyarVM 的底层算子：

### 基础算子

| Valkyrie Neural | NyarVM Tensor Dialect | 说明 |
|:---|:---|:---|
| `neural` 声明 | Tensor Dialect HIR | 网络结构定义 |
| `dense` 层 | MatMul + Add + Relu | 全连接层 |
| `conv2d` 层 | Conv2D 算子 | 卷积层 |
| `lstm` 层 | 自定义 LSTM 算子 | 循环层 |
| `softmax` | Softmax 算子 | 激活函数 |
| `Dense(ArrayND)` 类型 | Array 类型 | 多维数组表示 |
| `matmul` | MatMul 算子 | 矩阵乘法 |
| `reshape` | Reshape 算子 | 数组变形 |

### Transformer/LLM 算子

| Valkyrie Neural | NyarVM Tensor Dialect | 说明 |
|:---|:---|:---|
| `transformer_block` | FusedAttention + FFN | Transformer 块 |
| `embedding` | MatMul（查表优化） | 词嵌入 |
| `position_encoding` | PositionEncoding 算子 | 位置编码 |
| `rotary_position_encoding` | RotaryPositionEncoding 算子 | RoPE 旋转位置编码 |
| `rms_norm` | RmsNorm 算子 | RMS 归一化 |
| `layer_norm` | LayerNorm 算子 | 层归一化 |
| `silu` / `gelu` | 激活函数算子 | LLM 常用激活 |
| `repeater(N)` | EGraph 复制 + 逐层优化 | 编译期层复制 |

### 训练算子

| Valkyrie Neural | NyarVM Tensor Dialect | 说明 |
|:---|:---|:---|
| `training.optimizer` | OptimizerStep | 参数更新（SGD/Adam/AdamW） |
| `training.loss` | Loss | 损失函数（MSE/CrossEntropy） |
| `training.mode: lora` | LoRA 适配器节点 | 低秩适配微调 |
| `training.checkpoint` | Checkpoint | 梯度检查点（重计算策略） |
| `training.precision: bf16` | Cast + 混合精度节点 | 混合精度训练 |

### 融合算子（编译期自动生成）

| 融合规则 | 源模式 | 目标算子 | 条件 |
|:---|:---|:---|:---|
| 卷积+归一化融合 | `BatchNorm(Conv2D(x,w), ...)` | `FusedConvBnRelu` | 推理模式 |
| 注意力融合 | `Softmax(MatMul(Q, K^T)) * V` | `FusedAttention` | 推理模式 |
| 转置消除 | `Transpose(Transpose(x, p1), p2)` | `Transpose(x, compose(p1,p2))` | — |
| 梯度融合 | `Grad(Compose f g, x)` | 链式法则分解 | 减少中间张量 |
| Adam 特化 | `OptimizerStep Adam ...` | 展开为无循环计算 | 静态超参已知 |
| 重计算策略 | `Checkpoint segment` | `Recompute` 或 `Materialize` | 显存/计算权衡 |
| LoRA 融合 | `MatMul(x, B) + MatMul(x, A*B')` | `MatMul(x, B + A*B')` | 推理模式 |

## 编译管线

```
Valkyrie Neural AST
    │
    ▼
AstToIkunConverter
    │
    ├──→ 训练图（含反向传播节点）
    │       │
    │       ▼ EGraph 饱和优化
    │       · 梯度融合
    │       · Adam 更新特化
    │       · 重计算策略选择
    │       · LoRA 适配器注入
    │       │
    │       ▼ 成本模型提取
    │       │
    │       ▼ 代码生成
    │       ├──→ CUDA 训练内核（bf16 + 梯度累积 + 检查点）
    │       └──→ PyTorch/XLA 后端（训练框架集成）
    │
    └──→ 推理图（仅前向传播）
            │
            ▼ EGraph 饱和优化
            · 算子融合（ConvBnRelu / FusedAttention）
            · 部分求值（静态权重 → 特化内核）
            · 转置消除
            · KV-Cache 图特化
            · LoRA 权重融合
            · 量化常量折叠
            │
            ▼ 成本模型提取
            │
            ▼ 代码生成
            ├──→ CUDA 推理内核（f16 KV-Cache + Flash Attention）
            ├──→ Metal 推理内核（Apple Silicon 优化）
            ├──→ WebGPU 推理内核（浏览器端推理）
            ├──→ CPU 推理内核（量化 + SIMD 向量化）
            └──→ NPU 推理内核（远期）
```

## 与 Gnosis 的映射

| Valkyrie Neural | Gnosis | 说明 |
|:---|:---|:---|
| `neural` 声明 | Gnosis.AI 神经网络 | AI 子系统 |
| 推理 API | Gnosis.AI 推理引擎 | 运行时推理 |
| 流式生成 | Gnosis.AI 推理引擎 + Gnosis.Runtime 协程 | 异步逐 token 生成 |
| KV-Cache | Gnosis.AI 推理引擎 | 多轮对话缓存 |
| `Dense(ArrayND)` 类型 | Gnosis.AI 多维数组 | 数据表示 |
| 底层算子 | NyarVM Tensor Dialect | 编译期优化 |
| 训练循环 | Gnosis.AI 训练引擎 | 运行时训练 |
| LoRA 微调 | Gnosis.AI 适配器系统 | 低秩适配 |
| 梯度检查点 | Gnosis.AI 内存管理 | 显存优化 |

## 与 Valkyrie 其他扩展的协同

### 与 Template 扩展协同

利用 `<% ... %>` 元代码块，在编译期根据配置生成不同的网络架构：

```v
# 编译期根据目标平台选择模型规模
neural NPCBrain {
    input {
        observation: Dense(ArrayND<f32, [u32; 1]>);
    }

    output {
        action: Dense(ArrayND<f32, [u32; 1]>);
    }

    layers {
        dense(128, activation: relu)

        # 编译期条件：移动端用小模型，桌面端用大模型
        <% if @target == "mobile" { %>
            dense(64, activation: relu)
        <% } else { %>
            dense(256, activation: relu)
            dense(128, activation: relu)
        <% } %>

        dense(action_dim, activation: softmax)
    }
}
```

### 与 ECS 扩展协同

Neural 推理结果直接驱动 ECS 系统行为：

```v
neural EnemyAI {
    input {
        distance: f32;
        health: f32;
        threat: f32;
    }

    output {
        action: Dense(ArrayND<f32, [u32; 1]>);
    }

    layers {
        dense(64, activation: relu)
        dense(32, activation: relu)
        dense(4, activation: softmax)
    }
}

system EnemyAISystem {
    query all = Query.all(EnemyAI, Position, Health);

    on_update(frame: Frame) {
        loop entity in query.all {
            # Neural 推理直接在 ECS 系统中调用
            let decision = entity.enemy_ai.infer(
                distance = entity.position.distance_to(player.position),
                health = entity.health.current / entity.health.max,
                threat = entity.perceive_threat()
            );

            # 推理结果驱动行为
            match decision.argmax() {
                0 => entity.attack(player),
                1 => entity.flee(),
                2 => entity.patrol(),
                3 => entity.take_cover(),
            }
        }
    }
}
```

### 与 Async 扩展协同

LLM 推理是计算密集型操作，配合 `.await` 实现非阻塞推理：

```v
system DialogueSystem {
    on_update(frame: Frame) {
        loop entity in query.all(NPCDialogue, PlayerNearby) {
            # 异步推理，不阻塞游戏主线程
            let response = entity.npc_dialogue.generate(
                prompt: entity.dialogue_context,
                max_tokens: 64
            ).await;

            entity.show_dialogue(response);
        }
    }
}
```

### 与 Schema 扩展协同

训练数据使用 Schema 定义，确保类型安全：

```v
class DialogueExample {
    context: utf8,
    response: utf8,
    quality: f32,
}

[source("data/dialogues.jsonl")]
model DialogueData {
    example: &DialogueExample,
}
```

## 量化与部署

### 编译期量化

```v
# 编译期指定量化策略，生成量化后的推理内核
let model = StoryTellerLLM.load("models/storyteller.weights")
    .quantize(int4, group_size: 128);

# 量化模型推理，显存占用降至 1/4
let output = model.infer(token_ids: prompt);
```

| 量化策略 | 精度 | 显存占用 | 适用场景 |
|:---|:---|:---|:---|
| `f32` | 全精度 | 1× | 训练 |
| `f16` | 半精度 | 1/2 | 推理（GPU） |
| `bf16` | BF16 | 1/2 | 训练 + 推理 |
| `int8` | 8 位整数量化 | 1/4 | 推理（CPU/GPU） |
| `int4` | 4 位整数量化 | 1/8 | 推理（边缘设备） |

### 多平台部署

编译期根据目标平台自动选择最优后端：

```v
# 编译期自动选择
# CUDA 平台 → Flash Attention + f16 KV-Cache
# Metal 平台 → Metal Performance Shaders + ANE 加速
# WebGPU 平台 → WGSL Compute Shader
# CPU 平台 → 量化 + SIMD + 线程池并行
```

## 开发状态

| 功能 | 状态 |
|:---|:---|
| neural 声明语法 | 📋 计划中 |
| Array 类型 | 📋 计划中 |
| 推理 API | 📋 计划中 |
| 底层算子映射 | 📋 计划中 |
| Tensor Dialect 映射 | 📋 计划中 |
| GPU 推理后端 | 📋 计划中 |
| Transformer 声明 | 📋 计划中 |
| LLM 推理（prefill/decode） | 📋 计划中 |
| KV-Cache 管理 | 📋 计划中 |
| 流式生成 | 📋 计划中 |
| 训练支持 | 📋 远期计划 |
| LoRA 微调 | 📋 远期计划 |
| 混合精度训练 | 📋 远期计划 |
| 梯度检查点 | 📋 远期计划 |
| 编译期量化 | 📋 远期计划 |
| 多阶段训练/推理衔接 | 📋 远期计划 |
| 与 ECS/Async/Template 协同 | 📋 远期计划 |
