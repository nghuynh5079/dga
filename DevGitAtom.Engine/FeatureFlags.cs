using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace DevGitAtom.Engine;

public class FeatureFlags
{
    private static readonly Lazy<FeatureFlags> _instance = new Lazy<FeatureFlags>(() => new FeatureFlags());
    public static FeatureFlags Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, bool> _flags = new();
    private readonly string _configFilePath;
    private readonly object _saveLock = new object();

    private FeatureFlags()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevGitAtom");
        Directory.CreateDirectory(appData);
        _configFilePath = Path.Combine(appData, "feature_flags.json");

        // Default flags
        _flags["EnableRollback"] = true;
        _flags["EnableAutoStage"] = true;
        _flags["EnableRetry"] = true;
        _flags["EnableGitSafetyChecks"] = true;
        _flags["EnableAuditLog"] = true;
        _flags["EnableBranchProtection"] = true;

        Load();
    }

    private void Load()
    {
        if (File.Exists(_configFilePath))
        {
            try
            {
                var json = File.ReadAllText(_configFilePath);
                var loaded = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, bool>>(json);
                if (loaded != null)
                {
                    foreach (var kvp in loaded)
                    {
                        _flags[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch
            {
                // Ignore load errors, keep defaults
            }
        }
        else
        {
            Save();
        }
    }

    private void Save()
    {
        try
        {
            lock (_saveLock)
            {
                var json = JsonSerializer.Serialize(_flags, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
            }
        }
        catch
        {
            // Ignore save errors
        }
    }

    public bool IsEnabled(string flagName)
    {
        if (_flags.TryGetValue(flagName, out bool value))
        {
            return value;
        }
        return false;
    }

    public void SetFlag(string flagName, bool value)
    {
        _flags[flagName] = value;
        Save();
    }
}
