namespace DevGitAtom.Core.Atoms;

public class PreconditionAtom : IAtom
{
    public string Id => "precondition";
    public string DisplayName => "Assert Preconditions";
    public AtomCategory Category => AtomCategory.Safety;
    public bool Mutating => false;
    public string[] Requires => [];
    public string[] Provides => [];

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        // Check git exists
        var (exitCode, _) = await GitRunner.RunAsync("rev-parse --git-dir", context.Config.WorkingDir, output);
        if (exitCode != 0) throw new Exception("Kh\u00f4ng ph\u1ea3i Git repository h\u1ee3p l\u1ec7.");

        // Record original branch
        context.OriginalBranch = await GitRunner.GetCurrentBranchAsync(context.Config.WorkingDir);
        output.Report($"Nh\u00e1nh hi\u1ec7n t\u1ea1i: {context.OriginalBranch}");

        return AtomOutcome.Completed;
    }
}
