using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms.Atoms;

public class PushAtom : IAtom
{
    public string Id => "push";
    public string DisplayName => "Push";
    public AtomCategory Category => AtomCategory.Sync;

    public bool Mutating => true;
    public string[] Requires => ["Commit"];
    public string[] Provides => [];

    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "remote", DisplayName = "Remote (e.g. origin)", DefaultValue = "origin" },
        new AtomParameter { Key = "force", DisplayName = "Force Push (--force)", DefaultValue = "false" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        var remote = context.CurrentParameters.TryGetValue("remote", out var r) && !string.IsNullOrWhiteSpace(r) ? r : "origin";
        var isForce = context.CurrentParameters.TryGetValue("force", out var f) && f.Equals("true", StringComparison.OrdinalIgnoreCase);

        progress.Report($"\n>> [Push to {remote}]");
        
        var branch = await GitRunner.GetCurrentBranchAsync(context.Config.WorkingDir);
        var forceFlag = isForce ? " --force" : "";
        var cmd = $"push {remote} {branch}{forceFlag}";

        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, progress);

        if (exitCode != 0)
        {
            return AtomOutcome.Failed;
        }

        return AtomOutcome.Completed;
    }
}
