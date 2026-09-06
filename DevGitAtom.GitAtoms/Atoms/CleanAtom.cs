using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms;

public class CleanAtom : IAtom
{
    public string Id => "clean";
    public string DisplayName => "Clean (Xóa file rác)";
    public AtomCategory Category => AtomCategory.Safety;
    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => ["CleanRepo"];

    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "flags", DisplayName = "Cờ lệnh (vd: -fd)", DefaultValue = "-fd" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        var flags = context.CurrentParameters.TryGetValue("flags", out var f) && !string.IsNullOrWhiteSpace(f) ? f : "-fd";

        progress.Report($"\n>> [Git Clean {flags}]");
        
        // This is highly destructive, cannot be undone! 
        // We log a warning.
        progress.Report("\x1b[33mCảnh báo: Lệnh này xóa vĩnh viễn các file chưa tracked!\x1b[0m");

        var cmd = $"clean {flags}";

        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, progress);

        if (exitCode != 0)
        {
            return AtomOutcome.Failed;
        }

        return AtomOutcome.Completed;
    }

    public Task UndoAsync(WorkflowContext context, IProgress<string> progress)
    {
        progress.Report("   [!] Không thể Undo lệnh git clean. Dữ liệu đã bị xoá vĩnh viễn.");
        return Task.CompletedTask;
    }
}
