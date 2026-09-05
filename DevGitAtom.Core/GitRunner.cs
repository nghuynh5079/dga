using System.Diagnostics;

namespace DevGitAtom.Core;

public static class GitRunner
{
    public static async Task<(int ExitCode, string Output)> RunAsync(
        string arguments,
        string workingDir,
        IProgress<string>? output = null)
    {
        var psi = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Cannot start git process");

        var sb = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            sb.AppendLine(e.Data);
            output?.Report(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            sb.AppendLine(e.Data);
            output?.Report(e.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return (process.ExitCode, sb.ToString());
    }

    public static async Task<string> GetCurrentBranchAsync(string workingDir)
    {
        var (_, output) = await RunAsync("branch --show-current", workingDir);
        return output.Trim();
    }

    public static async Task<bool> HasUncommittedChangesAsync(string workingDir)
    {
        var (exitCode, _) = await RunAsync("status --porcelain", workingDir);
        var (_, output) = await RunAsync("status --porcelain", workingDir);
        return !string.IsNullOrWhiteSpace(output);
    }
}
