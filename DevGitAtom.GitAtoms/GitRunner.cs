using DevGitAtom.Contracts;
using System.Diagnostics;

namespace DevGitAtom.GitAtoms;

public static class GitRunner
{
    public static async Task<(int ExitCode, string Output)> RunAsync(
        string arguments,
        string workingDir,
        IProgress<string>? output = null,
        CancellationToken ct = default)
    {
        int lockRetries = 0;
        
        while (true)
        {
            var psi = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            
            // Dùng chính app này làm màn hình nhập password (thông qua tham số --askpass)
            var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(currentExe))
            {
                var batPath = Path.Combine(Path.GetTempPath(), "devgitatom_askpass.bat");
                File.WriteAllText(batPath, $"@echo off\n\"{currentExe}\" --askpass %*");
                
                psi.EnvironmentVariables["GIT_ASKPASS"] = batPath;
                psi.EnvironmentVariables["SSH_ASKPASS"] = batPath;
                psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
            }

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
            
            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { }
                return (-1, sb.ToString() + "\n[X] Tiến trình đã bị hủy bởi người dùng.");
            }

            // Xử lý đụng độ index.lock (Command Queue / Wait pattern)
            if (process.ExitCode != 0 && sb.ToString().Contains("index.lock"))
            {
                if (lockRetries < 5)
                {
                    lockRetries++;
                    output?.Report($"\n[LOCK] Repo đang bị khóa bởi tiến trình Git khác. Chờ và thử lại lần {lockRetries}/5...");
                    await Task.Delay(2000, ct);
                    continue; // Quay lại đầu vòng lặp while để thử lại lệnh
                }
            }

            var finalOutput = sb.ToString();
            if (process.ExitCode != 0)
            {
                finalOutput = GitErrorParser.TranslateError(finalOutput);
            }

            return (process.ExitCode, finalOutput);
        }
    }

    public static async Task<string> GetCurrentBranchAsync(string workingDir)
    {
        var (_, output) = await RunAsync("branch --show-current", workingDir);
        return output.Trim();
    }

    public static async Task<bool> HasUncommittedChangesAsync(string workingDir)
    {
        var (_, output) = await RunAsync("status --porcelain", workingDir);
        return !string.IsNullOrWhiteSpace(output);
    }
}



