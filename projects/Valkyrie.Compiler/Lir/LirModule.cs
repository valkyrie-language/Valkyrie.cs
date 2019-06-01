using Nyar.Assembler;

namespace Valkyrie.Compiler.Lir;

/// <summary>
/// 低层中间表示模块。
/// 当前以 `CgModule` 作为 `Nyar Standard IR` 的承载结构。
/// </summary>
public sealed class LirModule
{
    public LirModule(CgModule module)
    {
        Module = module;
    }

    public CgModule Module { get; }

    public string Name => Module.Name;
}
