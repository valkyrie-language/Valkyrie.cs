using System.Text.RegularExpressions;

namespace Legion.Tools;

public class LegionIgnore
{
    private readonly List<string> _patterns = new();
    private readonly string _filePath;

    public LegionIgnore(string directoryPath)
    {
        _filePath = Path.Combine(directoryPath, "legion.ignore");
        Load();
    }

    public void Load()
    {
        _patterns.Clear();

        if (!File.Exists(_filePath))
        {
            return;
        }

        var lines = File.ReadAllLines(_filePath);
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
            {
                continue;
            }

            _patterns.Add(trimmed);
        }
    }

    public void Save()
    {
        File.WriteAllLines(_filePath, _patterns);
    }

    public bool IsIgnored(string path)
    {
        string normalizedPath = path.Replace("\\", "/");
        string fileName = Path.GetFileName(normalizedPath);

        foreach (var pattern in _patterns)
        {
            if (MatchesPattern(normalizedPath, fileName, pattern))
            {
                return true;
            }
        }

        return false;
    }

    public void AddPattern(string pattern)
    {
        if (!_patterns.Contains(pattern))
        {
            _patterns.Add(pattern);
        }
    }

    public void RemovePattern(string pattern)
    {
        _patterns.Remove(pattern);
    }

    public List<string> GetPatterns()
    {
        return new List<string>(_patterns);
    }

    public bool Exists()
    {
        return File.Exists(_filePath);
    }

    private bool MatchesPattern(string path, string fileName, string pattern)
    {
        bool negated = pattern.StartsWith("!");
        if (negated)
        {
            pattern = pattern.Substring(1);
        }

        bool directoryOnly = pattern.EndsWith("/");
        if (directoryOnly)
        {
            pattern = pattern.TrimEnd('/');
        }

        bool matches = IsMatch(path, fileName, pattern, directoryOnly);

        return negated ? !matches : matches;
    }

    private bool IsMatch(string path, string fileName, string pattern, bool directoryOnly)
    {
        // 简单文件名匹配（如 *.log）
        if (pattern.StartsWith("*."))
        {
            string ext = pattern.Substring(1);
            if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // 精确文件名匹配
        if (!pattern.Contains("/") && !pattern.Contains("*") && !pattern.Contains("?"))
        {
            if (string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 目录名匹配
            if (directoryOnly)
            {
                var parts = path.Split('/');
                foreach (var part in parts)
                {
                    if (string.Equals(part, pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 通配符匹配
        string regexPattern = ConvertToRegex(pattern);

        try
        {
            if (Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase))
            {
                return true;
            }

            if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }
        catch
        {
            // 正则表达式无效时回退到简单匹配
        }

        return false;
    }

    private string ConvertToRegex(string pattern)
    {
        string regex = pattern;

        // 转义正则特殊字符（先转义反斜杠）
        regex = regex.Replace("\\", "\\\\");
        regex = regex.Replace(".", "\\.");
        regex = regex.Replace("+", "\\+");
        regex = regex.Replace("(", "\\(");
        regex = regex.Replace(")", "\\)");
        regex = regex.Replace("[", "\\[");
        regex = regex.Replace("]", "\\]");
        regex = regex.Replace("{", "\\{");
        regex = regex.Replace("}", "\\}");
        regex = regex.Replace("^", "\\^");
        regex = regex.Replace("$", "\\$");

        // 处理通配符（按从长到短的顺序）
        regex = regex.Replace("/**/", "/(.*/)?");
        regex = regex.Replace("**", ".*");
        regex = regex.Replace("*", "[^/]*");
        regex = regex.Replace("?", ".");

        // 锚定
        if (!pattern.StartsWith("/") && !pattern.StartsWith("*"))
        {
            regex = "(^|.*/)" + regex;
        }
        else if (pattern.StartsWith("/"))
        {
            regex = "^" + regex.Substring(1);
        }

        return regex + "$";
    }

    public static LegionIgnore CreateDefault(string directoryPath)
    {
        var ignore = new LegionIgnore(directoryPath);

        var defaultPatterns = new[]
        {
            "# Legion ignore file",
            "# Patterns follow gitignore syntax",
            "",
            "# Build outputs",
            "build/",
            "dist/",
            "bin/",
            "obj/",
            "",
            "# Dependencies (managed by Legion)",
            "vendors/",
            "",
            "# Cache",
            ".cache/",
            "",
            "# IDE",
            ".idea/",
            ".vscode/",
            "*.user",
            "",
            "# OS files",
            ".DS_Store",
            "Thumbs.db",
            "",
            "# Logs",
            "*.log",
            "logs/",
            "",
            "# Test outputs",
            "coverage/",
            ".nyc_output/",
            "",
            "# Legion internal",
            ".valkyrie/",
            "legion-lock.von"
        };

        ignore._patterns.AddRange(defaultPatterns.Where(p => !string.IsNullOrEmpty(p) && !p.StartsWith("#")));
        ignore.Save();

        return ignore;
    }
}