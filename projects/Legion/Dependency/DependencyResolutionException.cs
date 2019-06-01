namespace Legion.Dependency;

/// <summary>
///     依赖解析异常，用于解析过程中的可恢复错误
/// </summary>
public class DependencyResolutionException : Exception
{
    public DependencyResolutionException(string message) : base(message) { }
    public DependencyResolutionException(string message, Exception inner) : base(message, inner) { }
}