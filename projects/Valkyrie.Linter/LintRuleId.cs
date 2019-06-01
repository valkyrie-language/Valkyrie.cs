namespace Valkyrie.Linter;

/// <summary>
/// Lint 规则 ID 常量
/// </summary>
public static class LintRuleIds
{
    #region 代码质量规则

    /// <summary>
    /// 未使用的变量
    /// </summary>
    public const string UnusedVariable = "VALK_L001";

    /// <summary>
    /// 未使用的导入
    /// </summary>
    public const string UnusedImport = "VALK_L002";

    /// <summary>
    /// 过长的函数
    /// </summary>
    public const string LongFunction = "VALK_L003";

    /// <summary>
    /// 过深的嵌套
    /// </summary>
    public const string DeepNesting = "VALK_L004";

    /// <summary>
    /// 重复代码检测
    /// </summary>
    public const string DuplicateCode = "VALK_L005";

    /// <summary>
    /// 可变变量未修改
    /// </summary>
    public const string UnmodifiedMutVariable = "VALK_L006";

    /// <summary>
    /// 不安全的裸指针
    /// </summary>
    public const string UnsafeRawPointer = "VALK_L007";

    /// <summary>
    /// 未处理的 result
    /// </summary>
    public const string UnhandledResult = "VALK_L008";

    /// <summary>
    /// 过大的结构体
    /// </summary>
    public const string LargeStruct = "VALK_L009";

    #endregion

    #region ECS 规则

    /// <summary>
    /// 组件包含逻辑
    /// </summary>
    public const string ComponentWithLogic = "VALK_E001";

    /// <summary>
    /// 系统包含状态
    /// </summary>
    public const string SystemWithState = "VALK_E002";

    /// <summary>
    /// 查询未使用
    /// </summary>
    public const string UnusedQuery = "VALK_E003";

    /// <summary>
    /// System 缺少生命周期
    /// </summary>
    public const string MissingLifecycle = "VALK_E004";

    /// <summary>
    /// 循环依赖 Component
    /// </summary>
    public const string CircularComponentDependency = "VALK_E005";

    #endregion

    #region 安全规则

    /// <summary>
    /// 可变全局状态
    /// </summary>
    public const string MutableGlobalState = "VALK_S001";

    /// <summary>
    /// 未检查的可空值
    /// </summary>
    public const string UncheckedNullable = "VALK_S002";

    /// <summary>
    /// 不安全的类型转换
    /// </summary>
    public const string UnsafeCast = "VALK_S003";

    /// <summary>
    /// 除零检查缺失
    /// </summary>
    public const string DivisionByZero = "VALK_S004";

    /// <summary>
    /// 缓冲区溢出风险
    /// </summary>
    public const string BufferOverflowRisk = "VALK_S005";

    #endregion
}