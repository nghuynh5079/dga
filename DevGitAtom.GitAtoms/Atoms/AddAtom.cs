using DevGitAtom.Contracts;
namespace DevGitAtom.GitAtoms.Atoms;

public class AddAtom : IAtom
{
    public string Id => "add";
    public string DisplayName => "Stage Changes (Add)";
    public AtomCategory Category => AtomCategory.Worktree;
    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => ["Stage"];
    
    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "files", DisplayName = "Files to stage (e.g. . or file.txt)", DefaultValue = "." }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        var files = context.CurrentParameters.TryGetValue("files", out var f) && !string.IsNullOrWhiteSpace(f) ? f : ".";
        
        output.Report($"Staging files: {files}");
        var cmd = $"add {files}";
        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, output);
        
        return exitCode == 0 ? AtomOutcome.Completed : AtomOutcome.Failed;
    }

    public async Task UndoAsync(WorkflowContext context, IProgress<string> output)
    {
        var files = context.CurrentParameters.TryGetValue("files", out var f) && !string.IsNullOrWhiteSpace(f) ? f : ".";
        output.Report($"Undoing stage (resetting {files})...");
        
        // Dùng git reset HEAD <files> để unstage
        await GitRunner.RunAsync($"reset HEAD {files}", context.Config.WorkingDir, output);
    }
}
