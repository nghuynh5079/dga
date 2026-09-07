using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DevGitAtom.Engine.Logging;

public enum LogLevel
{
    TRACE,
    DEBUG,
    INFO,
    WARN,
    ERROR,
    FATAL
}

public class LogEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = "";

    [JsonPropertyName("level")]
    public string Level { get; set; } = "INFO";

    [JsonPropertyName("flow")]
    public string Flow { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("workspace")]
    public string? Workspace { get; set; }

    [JsonPropertyName("exception")]
    public string? Exception { get; set; } = null;

    [JsonPropertyName("data")]
    public object? Data { get; set; } = null;

    [JsonPropertyName("caller_file")]
    public string? CallerFile { get; set; }

    [JsonPropertyName("caller_method")]
    public string? CallerMethod { get; set; }

    [JsonPropertyName("caller_line")]
    public int? CallerLine { get; set; }
}

public static class JsonLogger
{
    private static readonly string LogDir = GetBaseLogDir();
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions { WriteIndented = false };
    private static readonly string _sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);

    private static string GetBaseLogDir()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevGitAtom", "Logs");
        Directory.CreateDirectory(appData);
        return appData;
    }

    static JsonLogger()
    {
        if (!Directory.Exists(LogDir))
        {
            Directory.CreateDirectory(LogDir);
        }
        Task.Run(PurgeOldLogs);
    }

    private static void PurgeOldLogs()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-30);
            foreach (var file in Directory.GetFiles(LogDir, "*_AppLog.jsonl"))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoff)
                {
                    fileInfo.Delete();
                }
            }
        }
        catch
        {
            // Ignore purge errors
        }
    }

    private static string GetLogFilePath()
    {
        var fileName = $"{DateTime.Now:yyyy_MM_dd}_AppLog.jsonl";
        return Path.Combine(LogDir, fileName);
    }

    private static void WriteLog(LogEntry entry)
    {
        entry.SessionId = _sessionId;
        try
        {
            var json = JsonSerializer.Serialize(entry, _options);
            Task.Run(async () =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    File.AppendAllText(GetLogFilePath(), json + Environment.NewLine);
                }
                catch
                {
                }
                finally
                {
                    _semaphore.Release();
                }
            });
        }
        catch
        {
            // Fail silently
        }
    }

    private static void Log(LogLevel level, string flow, string message, Exception? ex = null, object? data = null, string? workspace = null, string? callerFile = null, string? callerMethod = null, int callerLine = 0)
    {
        WriteLog(new LogEntry 
        { 
            Level = level.ToString(), 
            Flow = flow, 
            Message = message, 
            Workspace = workspace,
            Exception = ex?.ToString(),
            Data = data,
            CallerFile = Path.GetFileName(callerFile),
            CallerMethod = callerMethod,
            CallerLine = callerLine
        });
    }

    public static void LogInfo(string flow, string message, object? data = null, string? workspace = null,
        [CallerFilePath] string? callerFile = null,
        [CallerMemberName] string? callerMethod = null,
        [CallerLineNumber] int callerLine = 0)
    {
        Log(LogLevel.INFO, flow, message, null, data, workspace, callerFile, callerMethod, callerLine);
    }

    public static void LogWarn(string flow, string message, object? data = null, string? workspace = null,
        [CallerFilePath] string? callerFile = null,
        [CallerMemberName] string? callerMethod = null,
        [CallerLineNumber] int callerLine = 0)
    {
        Log(LogLevel.WARN, flow, message, null, data, workspace, callerFile, callerMethod, callerLine);
    }

    public static void LogError(string flow, string message, Exception? ex = null, object? data = null, string? workspace = null,
        [CallerFilePath] string? callerFile = null,
        [CallerMemberName] string? callerMethod = null,
        [CallerLineNumber] int callerLine = 0)
    {
        Log(LogLevel.ERROR, flow, message, ex, data, workspace, callerFile, callerMethod, callerLine);
    }

    public static void LogGitCommand(string command, string workingDir, int exitCode, string output,
        [CallerFilePath] string? callerFile = null,
        [CallerMemberName] string? callerMethod = null,
        [CallerLineNumber] int callerLine = 0)
    {
        var data = new { ExitCode = exitCode, Output = output, WorkingDir = workingDir };
        Log(exitCode == 0 ? LogLevel.INFO : LogLevel.WARN, "GitCommand", command, null, data, Path.GetFileName(workingDir), callerFile, callerMethod, callerLine);
    }
}
