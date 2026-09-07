using DevGitAtom.Contracts;
namespace DevGitAtom.GitAtoms.Atoms;

public class CommitAtom : IAtom
{
    public string Id => "commit";
    public string DisplayName => "Commit Changes";
    public AtomCategory Category => AtomCategory.History;
    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => ["Commit"];
    
    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "message", DisplayName = "Commit Message", DefaultValue = "Update" },
        new AtomParameter { Key = "flags", DisplayName = "Extra Flags (e.g. --amend)", DefaultValue = "" },
        new AtomParameter { Key = "autoStage", DisplayName = "Auto Stage Changes", DefaultValue = "true" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        var msg = context.CurrentParameters.TryGetValue("message", out var m) ? m : "Update";
        var flags = context.CurrentParameters.TryGetValue("flags", out var f) ? f : "";
        var autoStageStr = context.CurrentParameters.TryGetValue("autoStage", out var ast) ? ast : "true";
        bool.TryParse(autoStageStr, out var autoStage);
        
        var hasChanges = await GitRunner.HasUncommittedChangesAsync(context.Config.WorkingDir);
        if (!hasChanges && !flags.Contains("--amend"))
        {
            output.Report("Kh\u00f4ng c\u00f3 thay \u0111\u1ed5i \u0111\u1ec3 commit.");
            return AtomOutcome.Skipped;
        }

        if (autoStage)
        {
            output.Report("Staging all changes...");
            await GitRunner.RunAsync("add -A", context.Config.WorkingDir, output);
        }

        var cmd = $"commit {flags} -m \"{msg}\"".Trim();
        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, output);
        
        return exitCode == 0 ? AtomOutcome.Completed : AtomOutcome.Failed;
    }

    public async Task UndoAsync(WorkflowContext context, IProgress<string> output)
    {
        output.Report("Undoing commit (reset --soft HEAD~1)...");
        await GitRunner.RunAsync("reset --soft HEAD~1", context.Config.WorkingDir, output);
    }
}



