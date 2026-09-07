using DevGitAtom.Contracts;
namespace DevGitAtom.GitAtoms.Atoms;

public class FetchAtom : IAtom
{
    public string Id => "fetch";
    public string DisplayName => "Fetch Origin";
    public AtomCategory Category => AtomCategory.Sync;
    public bool Mutating => false; // Fetch không làm thay đổi working tree
    public string[] Requires => [];
    public string[] Provides => ["RemoteData"];

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        var (exitCode, _) = await GitRunner.RunAsync("fetch --all", context.Config.WorkingDir, output);
        if (exitCode != 0) throw new Exception("git fetch thất bại. Vui lòng kiểm tra lại cấu hình remote.");
        return AtomOutcome.Completed;
    }
}



