# 图像系统

## 概述

Acorn.Image 是 Acorn 二进制编解码生态系统中的统一图像门面。它为所有支持的格式提供了单一的解码、编码和处理 API。每种格式都有独立的 Acorn 项目（Acorn.Png、Acorn.Bmp、Acorn.Jpeg、Acorn.Tga、Acorn.Dds、Acorn.Exr、Acorn.Psd、Acorn.Gif），遵循"每种格式只有一个 Source"原则。

Acorn.Image 本身不实现任何格式的编解码逻辑，而是将请求委托给对应的格式项目。这种门面模式使得上层消费者（如 Gnosis 引擎）无需了解各格式的具体实现细节，只需面对一套统一的 API。

## 门面架构

```
┌─────────────────────────────────────────────────────────────┐
│                    上层消费者                                 │
│         Gnosis.Asset / Gnosis.Graphic / 其他工具             │
└──────────────────────────┬──────────────────────────────────┘
                           │ 统一 API
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                  Acorn.Image（门面层）                        │
│                                                             │
│  ImageDecoder    ImageEncoder    ImageProcessor              │
│  DetectFormat    RgbaImage       Resize / Mipmap / Alpha     │
└──┬───┬───┬───┬───┬───┬───┬───┬──┴───────────────────────────┘
   │   │   │   │   │   │   │   │
   ↓   ↓   ↓   ↓   ↓   ↓   ↓   ↓
┌─────┐┌─────┐┌──────┐┌─────┐┌─────┐┌──────┐┌──────┐┌──────┐
│Acorn││Acorn││Acorn ││Acorn││Acorn││Acorn ││Acorn ││Acorn │
│.Png ││.Bmp ││.Jpeg ││.Tga ││.Dds ││.Exr  ││.Psd  ││.Gif  │
└──┬──┘└──┬──┘└──┬───┘└──┬──┘└──┬──┘└──┬───┘└──┬───┘└──┬───┘
   │      │      │       │      │      │       │       │
   ↓      ↓      ↓       ↓      ↓      ↓       ↓       ↓
┌─────────────────────────────────────────────────────────────┐
│                    Acorn（核心基础设施）                       │
│            Frame / Codec / ByteBuffer / LEB128               │
└─────────────────────────────────────────────────────────────┘
```

每个格式项目均遵循标准的三件套结构：

- **Decode** — 字节流 → 数据结构
- **Encode** — 数据结构 → 字节流
- **Scanner** — 帧扫描与验证

部分格式项目还包含 **Data** 子命名空间，定义该格式的专有数据模型。

## 格式编解码器项目列表

| 格式 | 项目 | 解码 | 编码 | 扫描 | 用途 |
|:---|:---|:---|:---|:---|:---|
| PNG | Acorn.Png | ✅ | ✅ | ✅ | 通用无损压缩 |
| BMP | Acorn.Bmp | ✅ | ✅ | ✅ | Windows 位图 |
| JPEG | Acorn.Jpeg | ✅ | ✅ | ✅ | 有损压缩照片 |
| TGA | Acorn.Tga | ✅ | ✅ | ✅ | 游戏纹理 |
| DDS | Acorn.Dds | ✅ | ✅ | ✅ | DirectX 纹理压缩 |
| EXR | Acorn.Exr | ✅ | ✅ | ✅ | HDR 高动态范围 |
| PSD | Acorn.Psd | ✅ | ✅ | ✅ | Photoshop 工程文件 |
| GIF | Acorn.Gif | ✅ | ✅ | ✅ | 动画 |

## RgbaImage 统一数据模型

`RgbaImage` 是 Acorn.Image 中的核心数据模型，为所有格式提供统一的像素表示。

### 像素存储布局

- 像素以交织的 R、G、B、A 字节序列存储
- 扫描顺序：从左到右，从上到下
- 每个像素占 4 字节（R、G、B、A 各 1 字节）
- 数据总长度 = Width × Height × 4

### 内存布局示意

```
行 0:  [R0G0B0A0] [R1G1B1A1] [R2G2B2A2] ... [Rw-1Gw-1Bw-1Aw-1]
行 1:  [R0G0B0A0] [R1G1B1A1] [R2G2B2A2] ... [Rw-1Gw-1Bw-1Aw-1]
...
行 h-1: [R0G0B0A0] [R1G1B1A1] [R2G2B2A2] ... [Rw-1Gw-1Bw-1Aw-1]
```

### 核心成员

| 成员 | 类型 | 说明 |
|:---|:---|:---|
| `Width` | `int` | 图像宽度（像素） |
| `Height` | `int` | 图像高度（像素） |
| `Data` | `byte[]` | 像素数据，长度为 Width × Height × 4 |
| `GetPixel(int x, int y)` | `(byte R, byte G, byte B, byte A)` | 获取指定位置的像素 |
| `SetPixel(int x, int y, byte r, byte g, byte b, byte a)` | `void` | 设置指定位置的像素 |

### 关联枚举

**ImageFormat** — 指定图像的编解码格式：

| 值 | 说明 |
|:---|:---|
| `Png` | PNG 格式 |
| `Bmp` | BMP 格式 |
| `Jpeg` | JPEG 格式 |
| `Tga` | TGA 格式 |
| `Dds` | DDS 格式 |
| `Exr` | EXR 格式 |
| `Psd` | PSD 格式 |
| `Gif` | GIF 格式 |

**ResizeFilter** — 指定缩放时使用的滤波器：

| 值 | 说明 |
|:---|:---|
| `Nearest` | 最近邻插值，速度快，边缘锐利 |
| `Bilinear` | 双线性插值，平滑过渡 |
| `Lanczos3` | Lanczos3 插值，高质量，适合下采样 |

## ImageDecoder API

`ImageDecoder` 提供图像解码功能，支持自动格式检测和指定格式解码。

### 自动检测解码

```csharp
/// <summary>
/// 自动检测格式并解码图像数据
/// </summary>
/// <param name="data">图像二进制数据</param>
/// <returns>解码后的 RgbaImage</returns>
/// <exception cref="FormatException">无法识别的图像格式</exception>
public static RgbaImage Decode(ReadOnlySpan<byte> data)
```

该方法通过 `DetectFormat` 自动识别输入数据的格式，然后委托给对应格式的解码器。当格式无法识别时抛出 `FormatException`。

### 格式检测

```csharp
/// <summary>
/// 从魔数检测图像格式
/// </summary>
/// <param name="data">图像二进制数据</param>
/// <returns>检测到的格式，无法识别时返回 null</returns>
public static ImageFormat? DetectFormat(ReadOnlySpan<byte> data)
```

该方法读取数据开头的魔数字节，与各格式的签名进行匹配。返回 `ImageFormat?`，无法识别时返回 `null`。

### 指定格式解码

| 方法 | 说明 |
|:---|:---|
| `DecodePng(ReadOnlySpan<byte> data)` | 解码 PNG 图像 |
| `DecodeBmp(ReadOnlySpan<byte> data)` | 解码 BMP 图像 |
| `DecodeJpeg(ReadOnlySpan<byte> data)` | 解码 JPEG 图像 |
| `DecodeTga(ReadOnlySpan<byte> data)` | 解码 TGA 图像 |
| `DecodeDds(ReadOnlySpan<byte> data)` | 解码 DDS 图像 |
| `DecodeExr(ReadOnlySpan<byte> data)` | 解码 EXR 图像 |
| `DecodePsd(ReadOnlySpan<byte> data)` | 解码 PSD 图像 |
| `DecodeGif(ReadOnlySpan<byte> data)` | 解码 GIF 图像 |

当已知图像格式时，直接调用对应方法可跳过格式检测步骤，略微提升性能。每个方法内部委托给对应 Acorn.Xxx 项目的解码器实现。

## ImageEncoder API

`ImageEncoder` 提供图像编码功能，将 `RgbaImage` 编码为目标格式的二进制数据。

### 通用编码

```csharp
/// <summary>
/// 将图像编码为指定格式
/// </summary>
/// <param name="image">待编码的图像</param>
/// <param name="format">目标格式</param>
/// <param name="quality">编码质量（1-100），仅对有损格式有效，默认 85</param>
/// <returns>编码后的二进制数据</returns>
public static byte[] Encode(RgbaImage image, ImageFormat format, int quality = 85)
```

`quality` 参数仅对有损压缩格式（如 JPEG）有效。无损格式（如 PNG、BMP）忽略此参数。

### 指定格式编码

| 方法 | 说明 |
|:---|:---|
| `EncodePng(RgbaImage image)` | 编码为 PNG 格式 |
| `EncodeBmp(RgbaImage image)` | 编码为 BMP 格式 |
| `EncodeJpeg(RgbaImage image, int quality = 85)` | 编码为 JPEG 格式，可指定质量 |
| `EncodeTga(RgbaImage image)` | 编码为 TGA 格式 |

每个方法内部委托给对应 Acorn.Xxx 项目的编码器实现，生成符合格式规范的二进制数据。

## ImageProcessor API

`ImageProcessor` 提供图像处理功能，包括缩放、Mipmap 生成和 Alpha 通道检测。

### 缩放

```csharp
/// <summary>
/// 缩放图像到指定尺寸
/// </summary>
/// <param name="image">源图像</param>
/// <param name="width">目标宽度</param>
/// <param name="height">目标高度</param>
/// <param name="filter">缩放滤波器</param>
/// <returns>缩放后的新图像</returns>
public static RgbaImage Resize(RgbaImage image, int width, int height, ResizeFilter filter)
```

滤波器选择建议：

- `Nearest` — 像素艺术、UI 图标等需要保持锐利边缘的场景
- `Bilinear` — 一般用途，速度与质量的平衡
- `Lanczos3` — 纹理下采样、高质量缩放，计算开销较大

### Mipmap 生成

```csharp
/// <summary>
/// 生成 Mipmap 链
/// </summary>
/// <param name="image">源图像（第 0 级）</param>
/// <param name="filter">缩放滤波器</param>
/// <param name="maxLevels">最大级数，0 表示自动计算</param>
/// <returns>Mipmap 链，索引 0 为原始图像</returns>
public static RgbaImage[] GenerateMipmaps(RgbaImage image, ResizeFilter filter, int maxLevels = 0)
```

返回的数组中，索引 0 为原始图像，索引 1 为原始图像的 1/2，索引 2 为 1/4，依此类推。当 `maxLevels` 为 0 时，自动计算最大级数。

### Mip 级数计算

```csharp
/// <summary>
/// 计算给定尺寸下的最大 Mip 级数
/// </summary>
/// <param name="width">图像宽度</param>
/// <param name="height">图像高度</param>
/// <returns>最大 Mip 级数（含第 0 级）</returns>
public static int CalculateMipLevels(int width, int height)
```

计算公式为 `1 + floor(log2(max(width, height)))`。例如 256×256 的纹理有 9 级 Mipmap（0-8）。

### Alpha 通道检测

```csharp
/// <summary>
/// 检测像素数据中是否存在非不透明的 Alpha 值
/// </summary>
/// <param name="data">RGBA 交织像素数据</param>
/// <returns>存在非不透明像素时返回 true</returns>
public static bool HasAlphaChannel(byte[] data)
```

该方法遍历像素数据中的 Alpha 通道字节，检测是否存在小于 255 的值。用于在纹理上传时决定是否需要启用 Alpha 混合。

## 格式检测规则

各图像格式通过文件开头的魔数字节进行识别：

| 格式 | 偏移 | 魔数 | 说明 |
|:---|:---|:---|:---|
| PNG | 0 | `89 50 4E 47 0D 0A 1A 0A` | PNG 签名 |
| JPEG | 0 | `FF D8 FF` | JPEG SOI 标记 |
| BMP | 0 | `42 4D` | "BM" |
| DDS | 0 | `44 44 53 20` | "DDS " |
| EXR | 0 | `76 2F 31 01` | EXR 魔数 |
| PSD | 0 | `38 42 50 53` | "8BPS" |
| GIF | 0 | `47 49 46 38` | "GIF8" |
| TGA | — | 无固定魔数 | 通过 footer 或字段推断 |

### TGA 格式检测的特殊处理

TGA 格式没有固定的文件头魔数，`DetectFormat` 采用以下策略进行推断：

1. 检查文件末尾是否包含 TGA 2.0 Footer 签名（`TRUEVISION-XFILE`）
2. 若无 Footer，则解析文件头字段，验证 ID 长度、色彩映射类型和图像类型是否在合法范围内
3. 由于推断的可靠性低于其他格式，建议在已知格式时直接使用 `DecodeTga`

### 检测优先级

当多个格式的魔数可能重叠时，`DetectFormat` 按以下优先级依次匹配：

1. PNG（8 字节签名，最明确）
2. JPEG（3 字节 SOI 标记）
3. BMP（2 字节 "BM"）
4. DDS（4 字节 "DDS "）
5. EXR（4 字节魔数）
6. PSD（4 字节 "8BPS"）
7. GIF（4 字节 "GIF8"）
8. TGA（无魔数，最后尝试推断）

## 与 Gnosis 引擎的集成

Gnosis 游戏引擎通过 Acorn.Image 消费图像编解码能力，遵循"每种格式只有一个 Source"的架构原则。

### 纹理资产加载

Gnosis.Asset 通过 Acorn.Image 加载纹理资产：

```
资产文件（.png / .dds / .exr / ...）
    │
    ↓  Gnosis.Asset
    │
    ↓  Acorn.Image.ImageDecoder.Decode()
    │
    ↓  自动检测格式 → 委托对应 Acorn.Xxx 解码器
    │
  RgbaImage
    │
    ↓  Gnosis.Asset
    │
  TextureAsset（引擎内部表示）
```

### GPU 纹理上传

Gnosis.Graphic 使用 `RgbaImage` 作为纹理上传到 GPU 的中间表示：

```
RgbaImage
    │
    ↓  Gnosis.Graphic
    │
  GPU Texture Object
    - 像素数据上传至显存
    - 根据 HasAlphaChannel 决定混合模式
    - 根据 Mipmap 链设置 LOD 参数
```

### Mipmap 生成

游戏引擎中的纹理 LOD 依赖 `ImageProcessor` 生成 Mipmap 链：

```
RgbaImage（原始纹理）
    │
    ↓  ImageProcessor.GenerateMipmaps(image, ResizeFilter.Lanczos3)
    │
  RgbaImage[]（Mipmap 链）
    │
    ↓  Gnosis.Graphic
    │
  GPU Texture（完整 Mipmap 链上传）
```

### 特定格式的引擎用途

| 格式 | 引擎用途 | 说明 |
|:---|:---|:---|
| DDS | 压缩纹理 | 支持 BC 压缩格式，减少显存占用 |
| EXR | HDR 纹理 | 天空盒、环境贴图等高动态范围纹理 |
| PNG | 通用纹理 | UI 贴图、图标等无损资产 |
| TGA | 游戏纹理 | 传统游戏纹理格式，兼容旧资产 |
| PSD | 工作流集成 | 直接读取 Photoshop 工程文件，减少导出步骤 |

### SPIR-V 着色器集成

Gnosis.Graphic 的着色器管线通过 Acorn.SpirV 消费 SPIR-V 格式，与纹理系统协同工作：

```
GGShader 源码
    │
    ↓  Oak.Valkyrie（文本解码）
    │
  AST
    │
    ↓  Valkyrie（前端转换）
    │
  IKun EGraph
    │
    ↓  Nyar（优化 + 代码生成）
    │
  Acorn.SpirV.Data（SPIR-V 数据结构）
    │
    ↓  Acorn.SpirV.Encode（二进制编码）
    │
  .spv 字节流
    │
    ↓  Gnosis.Graphic
    │
  GPU Shader Program（采样 Acorn.Image 解码的纹理）
```

着色器通过采样器（Sampler）访问由 Acorn.Image 解码并上传至 GPU 的纹理数据。整个管线严格遵守架构边界：Oak 负责文本编解码，Acorn 负责二进制编解码，Nyar 负责分析与优化，Gnosis 负责引擎运行时。
