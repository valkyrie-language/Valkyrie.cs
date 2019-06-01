namespace Legion.Config;

public class LegionConfigDirectory
{
    private readonly string _configDir;
    private readonly string _legionConfigPath;
    private readonly string _valkyrieConfigPath;

    public LegionConfigDirectory(string projectDirectory)
    {
        _configDir = Path.Combine(projectDirectory, ".config", "legion");
        _legionConfigPath = Path.Combine(_configDir, "legion.von");
        _valkyrieConfigPath = Path.Combine(_configDir, "valkyrie.von");

        EnsureExists();
    }

    public string ConfigDirectory => _configDir;
    public string LegionConfigPath => _legionConfigPath;
    public string ValkyrieConfigPath => _valkyrieConfigPath;

    public void EnsureExists()
    {
        if (!Directory.Exists(_configDir))
        {
            Directory.CreateDirectory(_configDir);
        }
    }

    public bool HasLegionConfig()
    {
        return File.Exists(_legionConfigPath);
    }

    public bool HasValkyrieConfig()
    {
        return File.Exists(_valkyrieConfigPath);
    }

    public string? ReadLegionConfig()
    {
        return HasLegionConfig() ? File.ReadAllText(_legionConfigPath) : null;
    }

    public string? ReadValkyrieConfig()
    {
        return HasValkyrieConfig() ? File.ReadAllText(_valkyrieConfigPath) : null;
    }

    public void WriteLegionConfig(string content)
    {
        EnsureExists();
        File.WriteAllText(_legionConfigPath, content);
    }

    public void WriteValkyrieConfig(string content)
    {
        EnsureExists();
        File.WriteAllText(_valkyrieConfigPath, content);
    }
}