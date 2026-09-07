using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms.Atoms;

public class ResetAtom : IAtom
{
    public string Id => "reset";
    public string DisplayName => "Reset";
    public AtomCategory Category => AtomCategory.Safety;

    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => [];

    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "target", DisplayName = "Target (vd: HEAD~1)", DefaultValue = "HEAD" },
        new AtomParameter { Key = "mode", DisplayName = "Mode (--soft, --mixed, --hard)", DefaultValue = "--hard" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        var target = context.CurrentParameters.TryGetValue("target", out var t) && !string.IsNullOrWhiteSpace(t) ? t : "HEAD";
        var mode = context.CurrentParameters.TryGetValue("mode", out var m) && !string.IsNullOrWhiteSpace(m) ? m : "--hard";

        progress.Report($"\n>> [Reset {mode} to {target}]");
        
        var cmd = $"reset {mode} {target}";

        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, progress);

        if (exitCode != 0)
        {
            return AtomOutcome.Failed;
        }

        return AtomOutcome.Completed;
    }
}
