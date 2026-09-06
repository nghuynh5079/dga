using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevGitAtom.Contracts;

public class AppConfig
{
    [JsonIgnore]
    public string WorkingDir { get; set; } = Environment.CurrentDirectory;

    public string BranchPrefix { get; set; } = "dev-";
    public string BgColorHex { get; set; } = "";
    public List<string> RecentBranches { get; set; } = [];
    public List<string> FavoriteBranches { get; set; } = [];
    public Dictionary<string, string> BranchAliases { get; set; } = [];

    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path)) return new AppConfig();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, _opts) ?? new AppConfig();
    }

    public void Save(string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(this, _opts));
    }
}

