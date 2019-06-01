# Valkyrie.Tests 测试文档

Valkyrie.Tests 项目包含 Valkyrie 语言的词法分析器测试、语法分析器测试，以及 Valkyrie.PackageManager 的单元测试。

## 测试结构

```
Valkyrie.Tests/
├── GlobalUsings.cs                    # 全局 using（xunit）
├── LexerTests/
│   └── ValkyrieLexerTests.cs          # 词法分析器测试
├── ParserTests/
│   └── ValkyrieParserTests.cs         # 语法分析器测试
└── Valkyrie.Tests.csproj
```

PackageManager 的单元测试位于 `Valkyrie.PackageManager/LegionTests.cs`。

## 词法分析器测试（ValkyrieLexerTests）

基于 Oak.GGScript 词法分析器，验证 Valkyrie 语言的 Token 生成。

### 基础词法分析

| 测试 | 说明 |
|:---|:---|
| `EmptySource_ShouldReturnOnlyEof` | 空源码仅返回 EOF |
| `WhitespaceOnly_ShouldReturnOnlyEof` | 纯空白源码仅返回 EOF |

### 关键字识别

| 测试 | 说明 |
|:---|:---|
| `Keywords_ShouldBeRecognized` | 识别 `let`、`component`、`system`、`widget`、`plugin`、`import`、`export`、`return`、`if`、`else`、`loop`、`while`、`struct`、`foreach`、`match`、`case`、`end` |

### 字面量

| 测试 | 说明 |
|:---|:---|
| `BooleanLiterals_ShouldBeRecognized` | `true`、`false` 识别为 Literal |
| `NullLiteral_ShouldBeRecognized` | `null` 识别为 Literal |

### 数字

| 测试 | 说明 |
|:---|:---|
| `IntegerNumber_ShouldBeRecognized` | 整数 `42` |
| `FloatNumber_ShouldBeRecognized` | 浮点数 `3.14` |
| `HexNumber_ShouldBeRecognized` | 十六进制 `0xFF` |
| `ScientificNotation_ShouldBeRecognized` | 科学计数法 `1.5e10` |
| `NumberSuffix_ShouldBeRecognized` | 数字后缀 `1.0f`、`42i`、`100u` |

### 字符串

| 测试 | 说明 |
|:---|:---|
| `DoubleQuotedString_ShouldBeRecognized` | 双引号字符串 |
| `SingleQuotedString_ShouldBeRecognized` | 单引号字符串 |
| `StringWithEscapes_ShouldBeRecognized` | 转义字符 `\n`、`\t` |
| `UnterminatedString_ShouldReportError` | 未闭合字符串报错 |

### 标识符

| 测试 | 说明 |
|:---|:---|
| `Identifier_ShouldBeRecognized` | 识别 `myVar`、`_private`、`camelCase`、`PascalCase` |

### 运算符

| 测试 | 说明 |
|:---|:---|
| `ArithmeticOperators_ShouldBeRecognized` | `+`、`-`、`*`、`/`、`%` |
| `ComparisonOperators_ShouldBeRecognized` | `==`、`!=`、`<`、`>`、`<=`、`>=` |
| `AssignmentOperators_ShouldBeRecognized` | `=`、`+=`、`-=`、`*=`、`/=` |
| `LogicalOperators_ShouldBeRecognized` | `&&`、`||`、`!` |
| `ArrowOperators_ShouldBeRecognized` | `=>`、`->` |

### 分隔符与标点

| 测试 | 说明 |
|:---|:---|
| `Delimiters_ShouldBeRecognized` | `(`、`)`、`{`、`}`、`,`、`;`、`.` |
| `ColonPunctuation_ShouldBeRecognized` | `:`、`::` |

### 特性标注

| 测试 | 说明 |
|:---|:---|
| `Attribute_ShouldBeRecognized` | `[Serializable]`、`[Range(0, 100)]` |

### 注释

| 测试 | 说明 |
|:---|:---|
| `LineComment_ShouldBeSkipped` | `#` 行注释被跳过 |
| `BlockComment_ShouldBeSkipped` | `<# ... #>` 块注释被跳过 |
| `NestedBlockComment_ShouldBeSkipped` | 嵌套块注释被正确跳过 |

### 元代码块

| 测试 | 说明 |
|:---|:---|
| `MetaBlock_ShouldBeRecognized` | `<% code block %>` 识别为 MetaBlockStart |
| `MetaExpression_ShouldBeRecognized` | `<%= expression %>` 识别为 MetaExpression |

### 位置追踪

| 测试 | 说明 |
|:---|:---|
| `TokenPosition_ShouldTrackLineAndColumn` | Token 正确追踪行号和列号 |

### 错误处理

| 测试 | 说明 |
|:---|:---|
| `UnexpectedCharacter_ShouldReportError` | 非法字符 `@` 报错 |

### 综合测试

| 测试 | 说明 |
|:---|:---|
| `ComponentDeclaration_ShouldTokenize` | `component` 声明正确分词 |
| `FunctionDeclaration_ShouldTokenize` | `micro` 函数声明正确分词 |

## 语法分析器测试（ValkyrieParserTests）

基于 Oak.GGScript 语法分析器，验证 Valkyrie 语言的 AST 生成。

### 编译单元

| 测试 | 说明 |
|:---|:---|
| `EmptySource_ShouldReturnEmptyCompilationUnit` | 空源码返回空编译单元 |

### 组件声明

| 测试 | 说明 |
|:---|:---|
| `ComponentDecl_ShouldParse` | 解析 `component Position { x: f32; y: f32; }` |
| `ComponentDecl_WithAttribute_ShouldParse` | 解析带 `[Serializable]` 特性的组件 |
| `ComponentDecl_WithDefaultValue_ShouldParse` | 解析带默认值的字段 |

### 系统声明

| 测试 | 说明 |
|:---|:---|
| `SystemDecl_ShouldParse` | 解析 `system MovementSystem` 含 query 和 on_update |

### 函数声明

| 测试 | 说明 |
|:---|:---|
| `MicroDecl_ShouldParse` | 解析 `micro add(a: i32, b: i32): i32` |
| `MicroDecl_WithAttribute_ShouldParse` | 解析带 `[Export]` 特性的函数 |

### 变量声明

| 测试 | 说明 |
|:---|:---|
| `VariableDecl_ShouldParse` | 解析 `let x: i32 = 42;`（不可变） |
| `MutableVariableDecl_ShouldParse` | 解析 `let mut counter: i32 = 0;`（可变） |

### 导入声明

| 测试 | 说明 |
|:---|:---|
| `ImportDecl_ShouldParse` | 解析 `import Gnosis.ECS;` |
| `ImportDecl_WithAlias_ShouldParse` | 解析 `import Gnosis.ECS as ecs;` |

### Widget 声明

| 测试 | 说明 |
|:---|:---|
| `WidgetDecl_ShouldParse` | 解析 `widget Button` 含属性和 render 方法 |

### 表达式

| 测试 | 说明 |
|:---|:---|
| `BinaryExpression_ShouldParse` | 二元表达式 `1 + 2 * 3` |
| `ComparisonExpression_ShouldParse` | 比较表达式 `x > 0 && y < 100` |
| `MemberAccessExpression_ShouldParse` | 成员访问 `entity.position.x` |
| `CallExpression_ShouldParse` | 函数调用 `add(1, 2)` |
| `LambdaExpression_ShouldParse` | Lambda 表达式 `(it) => it * 2` |

### 语句

| 测试 | 说明 |
|:---|:---|
| `IfStatement_ShouldParse` | if-else 语句 |
| `LoopStatement_ShouldParse` | loop 遍历语句 |
| `WhileStatement_ShouldParse` | while 循环语句 |

### 类型注解

| 测试 | 说明 |
|:---|:---|
| `GenericType_ShouldParse` | 泛型类型 `list<i32>` |
| `NestedGenericType_ShouldParse` | 嵌套泛型 `map<string, list<i32>>` |

### 综合测试

| 测试 | 说明 |
|:---|:---|
| `MultipleDeclarations_ShouldParse` | 多个声明（import + component + micro） |
| `PluginDeclaration_ShouldParse` | `plugin PhysicsEngine` 含 requires_arch、provides_macros、provides_capabilities |

## PackageManager 单元测试（LegionTests.cs）

### SemanticVersion 测试

| 测试 | 说明 |
|:---|:---|
| `Parse_ShouldParseBasicVersion` | 解析基本版本 `1.2.3` |
| `Parse_ShouldParsePreReleaseVersion` | 解析预发布版本 `1.2.3-alpha.1` |
| `Parse_ShouldParseBuildMetadata` | 解析构建元数据 `1.2.3+build.123` |
| `Parse_ShouldParseVersionWithVPrefix` | 解析带 `v` 前缀的版本 |
| `TryParse_ShouldReturnFalseForInvalidVersion` | 无效版本返回 false |
| `CompareTo_ShouldOrderVersionsCorrectly` | 版本排序正确 |
| `CompareTo_PreReleaseShouldBeLessThanRelease` | 预发布版本低于正式版本 |
| `ToString_ShouldFormatCorrectly` | 格式化输出正确 |
| `Equals_ShouldCompareCorrectly` | 相等比较正确 |

### VersionRange 测试

| 测试 | 说明 |
|:---|:---|
| `Parse_ExactVersion_ShouldMatchExact` | 精确版本匹配 |
| `Parse_ExactVersion_ShouldNotMatchDifferent` | 精确版本不匹配不同版本 |
| `Parse_CaretRange_ShouldMatchCompatible` | `^` 范围匹配 |
| `Parse_TildeRange_ShouldMatchCompatible` | `~` 范围匹配 |
| `Parse_GreaterThanOrEqual_ShouldMatchCorrectly` | `>=` 范围匹配 |
| `Parse_LessThan_ShouldMatchCorrectly` | `<` 范围匹配 |
| `Parse_Wildcard_ShouldMatchAny` | `*` 匹配任意版本 |
| `Parse_Latest_ShouldMatchAny` | `latest` 匹配任意版本 |

### Legion 集成测试

| 测试 | 说明 |
|:---|:---|
| `InstallAsync_ShouldReturnPackage` | 安装包返回正确结果 |
| `UpdateAsync_ShouldReturnPackage` | 更新包返回正确结果 |
| `UninstallAsync_ShouldReturnPackage` | 卸载包返回正确结果 |
| `SearchAsync_ShouldReturnPackages` | 搜索包返回结果列表 |
| `RegisterRegistryEndpoint_ShouldRegisterCustomEndpoint` | 注册自定义端点 |
| `GetRegistry_ShouldReturnNullForUnknownRegistry` | 未知注册器返回 null |
| `InstallAsync_WithOrgPackage_ShouldExtractOrgName` | 作用域包正确提取组织名 |

### LockFile 测试

| 测试 | 说明 |
|:---|:---|
| `AddPackage_ShouldAddEntry` | 添加锁定条目 |
| `RemovePackage_ShouldRemoveEntry` | 移除锁定条目 |
| `SaveAndLoad_ShouldPersistData` | 保存和加载持久化 |

### PackageCache 测试

| 测试 | 说明 |
|:---|:---|
| `HasPackage_ShouldReturnFalseForMissingPackage` | 缓存中不存在返回 false |
| `AddPackage_ShouldCreateCacheEntry` | 添加缓存条目 |
| `RemovePackage_ShouldRemoveCacheEntry` | 移除缓存条目 |

### LegionConfig 测试

| 测试 | 说明 |
|:---|:---|
| `SetAndGet_ShouldWorkCorrectly` | 设置和获取配置项 |
| `Validate_ShouldReturnTrueForValidConfig` | 有效配置验证通过 |
| `Validate_ShouldReturnFalseForInvalidTimeout` | 无效超时验证失败 |

### SecurityAudit 测试

| 测试 | 说明 |
|:---|:---|
| `AuditPackageAsync_ShouldReturnResult` | 审计返回结果 |
| `IsLicenseCompatible_ShouldRecognizeCompatibleLicenses` | 兼容许可证识别 |
| `IsLicenseCompatible_ShouldRejectIncompatibleLicenses` | 不兼容许可证拒绝 |

### DependencyResolver 测试

| 测试 | 说明 |
|:---|:---|
| `ResolveAsync_ShouldReturnDependencyNode` | 解析返回依赖节点 |
| `ResolveAsync_ShouldResolveDependencies` | 递归解析子依赖 |

### PackagePublisher 测试

| 测试 | 说明 |
|:---|:---|
| `ValidatePackage_ShouldReturnTrueForValidOptions` | 有效包验证通过 |
| `ValidatePackage_ShouldReturnFalseForInvalidVersion` | 无效版本验证失败 |

### LegionManifest 测试

| 测试 | 说明 |
|:---|:---|
| `CreateAndSave_ShouldPersistManifest` | 创建并保存清单 |
| `Validate_ShouldReturnTrueForValidManifest` | 有效清单验证通过 |
| `Validate_ShouldReturnFalseForEmptyName` | 空名称验证失败 |
| `Validate_ShouldReturnFalseForInvalidVersion` | 无效版本验证失败 |
| `GetScript_ShouldReturnScriptContent` | 获取脚本内容 |
| `HasScript_ShouldReturnFalseForMissingScript` | 不存在的脚本返回 false |

### LegionsWorkspace 测试

| 测试 | 说明 |
|:---|:---|
| `CreateAndSave_ShouldPersistWorkspace` | 创建并保存工作区 |
| `AddMember_ShouldNotDuplicate` | 重复添加成员被忽略 |

### LegionIgnore 测试

| 测试 | 说明 |
|:---|:---|
| `IsIgnored_ShouldMatchPattern` | 模式匹配正确 |
| `CreateDefault_ShouldCreateStandardPatterns` | 默认模式创建正确 |

### ScriptRunner 测试

| 测试 | 说明 |
|:---|:---|
| `RunAsync_ShouldExecuteCommand` | 执行命令成功 |
| `RunScriptAsync_ShouldReturnErrorForMissingScript` | 不存在的脚本返回错误 |

### LegionConfigDirectory 测试

| 测试 | 说明 |
|:---|:---|
| `EnsureExists_ShouldCreateDirectory` | 确保目录存在 |
| `WriteAndReadLegionConfig_ShouldPersist` | 读写配置持久化 |

### Legion 工作模式测试

| 测试 | 说明 |
|:---|:---|
| `Constructor_WithWorkspaceFile_ShouldDetectWorkspaceMode` | 检测 Workspace 模式 |
| `Constructor_WithManifestFile_ShouldDetectPackageMode` | 检测 Package 模式 |
| `Constructor_WithNoManifest_ShouldDetectStandaloneMode` | 检测 Script 模式 |
| `InstallAsync_InPackageMode_ShouldUseManifestDependencies` | Package 模式使用清单依赖 |
| `InstallAsync_InWorkspaceMode_ShouldReturnEmptyWithoutMembers` | 无成员工作区安装返回空 |
| `RunAsync_InStandaloneMode_ShouldExecuteDirectCommand` | Script 模式直接执行命令 |
| `RunAsync_InPackageMode_ShouldExecuteManifestScript` | Package 模式执行清单脚本 |

### YearlyVersion 测试

| 测试 | 说明 |
|:---|:---|
| `Parse_ShouldParseBasicVersion` | 解析基本版本 `2024.1.0.0` |
| `Parse_ShouldParseVersionWithBuildInfo` | 解析带构建信息的版本 |
| `Parse_ShouldThrowForInvalidFormat` | 无效格式抛出异常 |
| `TryParse_ShouldReturnFalseForInvalidVersion` | 无效版本返回 false |
| `IsResearch_ShouldReturnTrueForYearlyZero` | `yearly = 0` 为预研版 |
| `IsBeta_ShouldReturnTrueForMajorZero` | `major = 0` 为测试版 |
| `IsStable_ShouldReturnTrueForYearlyAndMajorNonZero` | 稳定版判断 |
| `CompareTo_ShouldOrderVersionsCorrectly` | 版本排序 |
| `Equals_ShouldCompareCorrectly` | 相等比较 |
| `ToString_ShouldFormatCorrectly` | 格式化输出 |
| `YearlyVersionRange_ExactMatch_ShouldMatchExact` | 精确匹配 |
| `YearlyVersionRange_PatchWildcard_ShouldMatchAnyPatch` | patch 通配 |
| `YearlyVersionRange_MinorWildcard_ShouldMatchAnyMinorAndPatch` | minor 通配 |
| `YearlyVersionRange_MajorWildcard_ShouldMatchAnyMajorMinorPatch` | major 通配 |
| `YearlyVersionRange_AllWildcard_ShouldMatchAny` | 全通配 |
| `YearlyVersionRange_YearlyOnly_ShouldMatchAnyMajorMinorPatch` | 仅 yearly 匹配 |
| `YearlyVersionRange_YearlyMajor_ShouldMatchAnyMinorPatch` | yearly.major 匹配 |
| `YearlyVersionRange_BuildInfo_ShouldParticipateInComparison` | BuildInfo 参与比较 |
| `YearlyVersionRange_InclusiveOrHigher_*` | `+` 后缀最低版本匹配（4 个测试） |
| `YearlyVersionRange_InclusiveOrHigher_SkipVulnerableVersion` | 跳过有漏洞版本 |

## 运行测试

```bash
# 运行所有测试
dotnet test d:\RiderProjects\Valkyrie.cs\Valkyrie.sln

# 运行 PackageManager 测试
dotnet test d:\RiderProjects\Valkyrie.cs\projects\Valkyrie.PackageManager\Valkyrie.PackageManager.csproj

# 运行语言前端测试
dotnet test d:\RiderProjects\Valkyrie.cs\projects\Valkyrie.Tests\Valkyrie.Tests.csproj
```
