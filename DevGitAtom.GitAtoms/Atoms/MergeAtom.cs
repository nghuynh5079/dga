using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms;

public class MergeAtom : IAtom
{
    public string Id => "merge";
    public string DisplayName => "Merge";
    public AtomCategory Category => AtomCategory.Worktree;

    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => [];

    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "sourceBranch", DisplayName = "Nhánh cần gộp vào", DefaultValue = "main" },
        new AtomParameter { Key = "noFF", DisplayName = "No Fast-Forward (--no-ff)", DefaultValue = "false" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        var sourceBranch = context.CurrentParameters.TryGetValue("sourceBranch", out var s) && !string.IsNullOrWhiteSpace(s) ? s : "main";
        var isNoFF = context.CurrentParameters.TryGetValue("noFF", out var nf) && nf.Equals("true", StringComparison.OrdinalIgnoreCase);

        progress.Report($"\n>> [Merge {sourceBranch}]");
        
        var noFfFlag = isNoFF ? " --no-ff" : "";
        var cmd = $"merge {sourceBranch}{noFfFlag}";

        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, progress);

        if (exitCode != 0)
        {
            return AtomOutcome.Failed;
        }

        return AtomOutcome.Completed;
    }

    public async Task UndoAsync(WorkflowContext context, IProgress<string> progress)
    {
        progress.Report("   Resetting merge (quay về ORIG_HEAD)...");
        await GitRunner.RunAsync("reset --merge ORIG_HEAD", context.Config.WorkingDir, progress);
    }
}
