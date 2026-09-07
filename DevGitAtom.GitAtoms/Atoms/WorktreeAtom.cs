using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms.Atoms;

public class WorktreeAtom : IAtom
{
    public string Id => "worktree_add";
    public string DisplayName => "Add Worktree";
    public AtomCategory Category => AtomCategory.Worktree;
    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => [];
    
    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "path", DisplayName = "Worktree Path", DefaultValue = "../worktree" },
        new AtomParameter { Key = "branch", DisplayName = "Branch", DefaultValue = "" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        var path = context.CurrentParameters.TryGetValue("path", out var p) ? p : "../worktree";
        var branch = context.CurrentParameters.TryGetValue("branch", out var b) ? b : "";
        
        if (string.IsNullOrWhiteSpace(branch))
        {
            output.Report("Error: Branch parameter is empty.");
            return AtomOutcome.Failed;
        }
        
        var (exitCode, _) = await GitRunner.RunAsync($"worktree add \"{path}\" {branch}", context.Config.WorkingDir, output);
        return exitCode == 0 ? AtomOutcome.Completed : AtomOutcome.Failed;
    }

    public async Task UndoAsync(WorkflowContext context, IProgress<string> output)
    {
        var path = context.CurrentParameters.TryGetValue("path", out var p) ? p : "../worktree";
        await GitRunner.RunAsync($"worktree remove \"{path}\" --force", context.Config.WorkingDir, output);
    }
}
