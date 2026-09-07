using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms.Atoms;

public class LogAtom : IAtom
{
    public string Id => "log";
    public string DisplayName => "Git Log";
    public AtomCategory Category => AtomCategory.History;

    public bool Mutating => false;
    public string[] Requires => [];
    public string[] Provides => [];

    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "count", DisplayName = "Số lượng commit", DefaultValue = "5" },
        new AtomParameter { Key = "oneline", DisplayName = "One line (--oneline)", DefaultValue = "true" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        var countStr = context.CurrentParameters.TryGetValue("count", out var c) ? c : "5";
        var isOneline = context.CurrentParameters.TryGetValue("oneline", out var o) && o.Equals("true", StringComparison.OrdinalIgnoreCase);

        if (!int.TryParse(countStr, out int count) || count <= 0) count = 5;

        progress.Report($"\n>> [Git Log ({count} commits)]");
        
        var flag = isOneline ? " --oneline" : "";
        var cmd = $"log -n {count}{flag}";

        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, progress);

        if (exitCode != 0)
        {
            return AtomOutcome.Failed;
        }

        return AtomOutcome.Completed;
    }
}
