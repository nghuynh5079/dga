namespace DevGitAtom.Core.Atoms;

public class StashAtom : IAtom
{
    public string Id => "stash";
    public string DisplayName => "Safe Stash";
    public AtomCategory Category => AtomCategory.Worktree;
    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => ["StashRef"];

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        var hasChanges = await GitRunner.HasUncommittedChangesAsync(context.Config.WorkingDir);
        if (!hasChanges)
        {
            output.Report("Working tree s\u1ea1ch, kh\u00f4ng c\u1ea7n stash.");
            context.LastAtomSkipped = true;
            return AtomOutcome.Skipped;
        }

        // Get hash before stash
        var (_, beforeHash) = await GitRunner.RunAsync("rev-parse -q --verify refs/stash", context.Config.WorkingDir);
        beforeHash = beforeHash.Trim();

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        await GitRunner.RunAsync($"stash push --include-untracked -m \"dev-git: {timestamp}\"", context.Config.WorkingDir, output);

        var (_, afterHash) = await GitRunner.RunAsync("rev-parse -q --verify refs/stash", context.Config.WorkingDir);
        afterHash = afterHash.Trim();

        if (!string.IsNullOrEmpty(afterHash) && afterHash != beforeHash)
        {
            context.StashRef = afterHash;
            output.Report($"Stash l\u01b0u t\u1ea1i: {afterHash[..8]}");
        }

        return AtomOutcome.Completed;
    }
}
