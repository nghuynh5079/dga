using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace DevGitAtom.Engine.Logging;

public class LogEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("level")]
    public string Level { get; set; } = "INFO";

    [JsonPropertyName("flow")]
    public string Flow { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("exception")]
    public string? Exception { get; set; } = null;

    [JsonPropertyName("data")]
    public object? Data { get; set; } = null;
}

public static class JsonLogger
{
    private static readonly string LogDir = GetBaseLogDir();

    private static string GetBaseLogDir()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevGitAtom", "Logs");
        Directory.CreateDirectory(appData);
        return appData;
    }
    private static readonly object _lock = new object();
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions { WriteIndented = false };

    static JsonLogger()
    {
        if (!Directory.Exists(LogDir))
        {
            Directory.CreateDirectory(LogDir);
        }
    }

    private static string GetLogFilePath()
    {
        // Format: yyyy_MM_dd_GitErrorLogging.jsonl (Ghi log theo ngày thay vì theo phút để tránh tạo quá nhiều file)
        var fileName = $"{DateTime.Now:yyyy_MM_dd}_GitErrorLogging.jsonl";
        return Path.Combine(LogDir, fileName);
    }

    private static void WriteLog(LogEntry entry)
    {
        try
        {
            var json = JsonSerializer.Serialize(entry, _options);
            lock (_lock)
            {
                File.AppendAllText(GetLogFilePath(), json + Environment.NewLine);
            }
        }
        catch
        {
            // Fail silently if logger fails
        }
    }

    public static void LogInfo(string flow, string message, object? data = null)
    {
        WriteLog(new LogEntry { Level = "INFO", Flow = flow, Message = message, Data = data });
    }

    public static void LogError(string flow, string message, Exception? ex = null, object? data = null)
    {
        WriteLog(new LogEntry 
        { 
            Level = "ERROR", 
            Flow = flow, 
            Message = message, 
            Exception = ex?.ToString(),
            Data = data 
        });
    }
}
