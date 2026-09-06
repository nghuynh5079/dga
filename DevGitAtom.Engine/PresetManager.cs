using DevGitAtom.Contracts;
using System.IO;
using System.Text.Json;

namespace DevGitAtom.Engine;

public class PresetManager
{
    private readonly string _presetsDir;
    private readonly object _lock = new object();

    public PresetManager()
    {
        _presetsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevGitAtom", "Presets");
        Directory.CreateDirectory(_presetsDir);
    }

    public void SavePreset(string name, List<ChainStep> chain)
    {
        lock (_lock)
        {
            var path = Path.Combine(_presetsDir, $"{name}.json");
            var json = JsonSerializer.Serialize(chain, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    public List<ChainStep> LoadPreset(string name)
    {
        lock (_lock)
        {
            var path = Path.Combine(_presetsDir, $"{name}.json");
            if (!File.Exists(path)) return new List<ChainStep>();
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<List<ChainStep>>(json) ?? new List<ChainStep>();
        }
    }

    public List<string> GetAvailablePresets()
    {
        lock (_lock)
        {
            if (!Directory.Exists(_presetsDir)) return new List<string>();
            return Directory.GetFiles(_presetsDir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .ToList()!;
        }
    }
    
    public void DeletePreset(string name)
    {
        lock (_lock)
        {
            var path = Path.Combine(_presetsDir, $"{name}.json");
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public void RenamePreset(string oldName, string newName)
    {
        lock (_lock)
        {
            var oldPath = Path.Combine(_presetsDir, $"{oldName}.json");
            var newPath = Path.Combine(_presetsDir, $"{newName}.json");
            if (File.Exists(oldPath)) File.Move(oldPath, newPath, true);
        }
    }
}


