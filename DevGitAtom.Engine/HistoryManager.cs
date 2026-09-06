using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DevGitAtom.Contracts;

namespace DevGitAtom.Engine;

public class RunRecord
{
    public DateTime Timestamp { get; set; }
    public string Workspace { get; set; } = "";
    public string ChainSummary { get; set; } = "";
    public bool IsSuccess { get; set; }
    public string Details { get; set; } = "";
}

public class HistoryManager
{
    private readonly string _historyFile;
    private readonly object _lock = new object();

    public HistoryManager()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevGitAtom");
        Directory.CreateDirectory(appData);
        _historyFile = Path.Combine(appData, "history.json");
    }

    public void AddRecord(RunRecord record)
    {
        lock (_lock)
        {
            var history = GetHistoryInternal();
            history.Insert(0, record);
            
            if (history.Count > 100) history = history.Take(100).ToList();

            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyFile, json);
        }
    }

    public List<RunRecord> GetHistory()
    {
        lock (_lock)
        {
            return GetHistoryInternal();
        }
    }

    private List<RunRecord> GetHistoryInternal()
    {
        if (!File.Exists(_historyFile)) return new List<RunRecord>();
        try
        {
            using var fs = new FileStream(_historyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<List<RunRecord>>(json) ?? new List<RunRecord>();
        }
        catch
        {
            return new List<RunRecord>();
        }
    }
}
