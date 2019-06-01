namespace Asgard.CLI;

public sealed class VoaHotReloadConfig
{
    public bool Enabled { get; set; } = true;
    public List<string> Watch { get; set; } = ["source/", "assets/"];
    public List<string> Ignore { get; set; } = [".git/", "node_modules/"];
    public int Debounce { get; set; } = 100;
}