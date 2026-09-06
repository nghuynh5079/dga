using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms;

public class RebaseAtom : IAtom
{
    public string Id => "rebase";
    public string DisplayName => "Rebase";
    public AtomCategory Category => AtomCategory.Worktree;

    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => [];

    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "targetBranch", DisplayName = "Nhánh gốc để rebase", DefaultValue = "main" },
        new AtomParameter { Key = "interactive", DisplayName = "Interactive (--interactive)", DefaultValue = "false" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        var targetBranch = context.CurrentParameters.TryGetValue("targetBranch", out var t) && !string.IsNullOrWhiteSpace(t) ? t : "main";
        var isInteractive = context.CurrentParameters.TryGetValue("interactive", out var i) && i.Equals("true", StringComparison.OrdinalIgnoreCase);

        progress.Report($"\n>> [Rebase onto {targetBranch}]");
        
        var flag = isInteractive ? " -i" : "";
        var cmd = $"rebase{flag} {targetBranch}";

        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, progress);

        if (exitCode != 0)
        {
            return AtomOutcome.Failed;
        }

        return AtomOutcome.Completed;
    }
}
