using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms;

public class RevertAtom : IAtom
{
    public string Id => "revert";
    public string DisplayName => "Revert Commit";
    public AtomCategory Category => AtomCategory.History;
    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => [];

    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "commit", DisplayName = "Commit ID cần revert", DefaultValue = "HEAD" },
        new AtomParameter { Key = "noCommit", DisplayName = "No Commit (--no-commit)", DefaultValue = "false" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        var commitId = context.CurrentParameters.TryGetValue("commit", out var c) && !string.IsNullOrWhiteSpace(c) ? c : "HEAD";
        var isNoCommit = context.CurrentParameters.TryGetValue("noCommit", out var nc) && nc.Equals("true", StringComparison.OrdinalIgnoreCase);

        progress.Report($"\n>> [Revert {commitId}]");
        
        var noCommitFlag = isNoCommit ? " --no-commit" : "";
        var cmd = $"revert {commitId}{noCommitFlag}";

        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, progress);

        if (exitCode != 0)
        {
            return AtomOutcome.Failed;
        }

        context.Data["RevertAtom_IsNoCommit"] = isNoCommit;
        return AtomOutcome.Completed;
    }

    public async Task UndoAsync(WorkflowContext context, IProgress<string> progress)
    {
        var isNoCommit = context.Data.TryGetValue("RevertAtom_IsNoCommit", out var val) && val is bool b && b;
        
        if (isNoCommit)
        {
            progress.Report("   Xóa các thay đổi revert trong working tree...");
            await GitRunner.RunAsync("reset --hard HEAD", context.Config.WorkingDir, progress);
        }
        else
        {
            progress.Report("   Xóa commit revert vừa tạo (reset --hard HEAD~1)...");
            await GitRunner.RunAsync("reset --hard HEAD~1", context.Config.WorkingDir, progress);
        }
    }
}
