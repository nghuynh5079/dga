using System;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms.Atoms;

public class DryRunAtom : IAtom
{
    public string Id => "dry_run";
    public string DisplayName => "Dry Run (Preview Commands)";
    public AtomCategory Category => AtomCategory.Safety;
    public bool Mutating => false;
    public string[] Requires => [];
    public string[] Provides => [];

    public Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        context.Data["DryRunMode"] = true;
        output.Report("[DRY-RUN MODE ENABLED] Các bước tiếp theo sẽ được preview, không thực thi thực tế.");
        return Task.FromResult(AtomOutcome.Completed);
    }
}
