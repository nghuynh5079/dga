using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DevGitAtom.Engine;

public class WorkspaceManager
{
    private readonly string _settingsFile;
    private readonly object _lock = new object();

    public WorkspaceManager()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevGitAtom");
        Directory.CreateDirectory(appData);
        _settingsFile = Path.Combine(appData, "workspaces.json");
    }

    public List<string> GetRecentWorkspaces()
    {
        lock (_lock)
        {
            return GetRecentWorkspacesInternal();
        }
    }

    private List<string> GetRecentWorkspacesInternal()
    {
        if (!File.Exists(_settingsFile)) return new List<string>();
        try
        {
            using var fs = new FileStream(_settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void AddWorkspace(string path)
    {
        lock (_lock)
        {
            var workspaces = GetRecentWorkspacesInternal();
            // Remove if exists to move to top
            workspaces.RemoveAll(w => w.Equals(path, StringComparison.OrdinalIgnoreCase));
            workspaces.Insert(0, path);

            // Keep top 10
            if (workspaces.Count > 10)
            {
                workspaces = workspaces.Take(10).ToList();
            }

            var json = JsonSerializer.Serialize(workspaces, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFile, json);
        }
    }
}
