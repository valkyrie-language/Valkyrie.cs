namespace Asgard.CLI;

public sealed class VoaBuildConfig
{
    public string Output { get; set; } = "dist";
    public bool Minify { get; set; } = true;
    public bool Sourcemap { get; set; } = true;
    public bool GenerateWat { get; set; } = false;
}
