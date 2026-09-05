namespace DevGitAtom.Core.Atoms;

public class StatusAtom : IAtom
{
    public string Id => "status";
    public string DisplayName => "Show Git Status";
    public AtomCategory Category => AtomCategory.Info;
    public bool Mutating => false;
    public string[] Requires => [];
    public string[] Provides => [];

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        await GitRunner.RunAsync("status", context.Config.WorkingDir, output);
        return AtomOutcome.Completed;
    }
}
