namespace DevGitAtom.Core.Atoms;

public class FetchAtom : IAtom
{
    public string Id => "fetch";
    public string DisplayName => "Fetch Origin";
    public AtomCategory Category => AtomCategory.Sync;
    public bool Mutating => false;
    public string[] Requires => [];
    public string[] Provides => [];

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        var (exitCode, _) = await GitRunner.RunAsync("fetch origin", context.Config.WorkingDir, output);
        if (exitCode != 0) throw new Exception("git fetch th\u1ea5t b\u1ea1i.");
        return AtomOutcome.Completed;
    }
}
