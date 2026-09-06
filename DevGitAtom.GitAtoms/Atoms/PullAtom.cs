using DevGitAtom.Contracts;
namespace DevGitAtom.GitAtoms;

public class PullAtom : IAtom
{
    public string Id => "pull";
    public string DisplayName => "Pull (--ff-only)";
    public AtomCategory Category => AtomCategory.Sync;
    public bool Mutating => true;
    public string[] Requires => ["RemoteData"];
    public string[] Provides => [];

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        var (exitCode, _) = await GitRunner.RunAsync("pull --ff-only", context.Config.WorkingDir, output);
        if (exitCode != 0) throw new Exception("Pull th\u1ea5t b\u1ea1i. Branch c\u00f3 th\u1ec3 \u0111\u00e3 diverge \u2014 kh\u00f4ng t\u1ef1 merge.");
        return AtomOutcome.Completed;
    }
}



