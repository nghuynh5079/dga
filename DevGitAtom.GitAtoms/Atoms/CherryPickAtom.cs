using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms.Atoms;

public class CherryPickAtom : IAtom
{
    public string Id => "cherrypick";
    public string DisplayName => "Cherry-Pick";
    public AtomCategory Category => AtomCategory.History;
    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => [];

    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "commit", DisplayName = "Commit ID cần nhặt", DefaultValue = "" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        var commitId = context.CurrentParameters.TryGetValue("commit", out var c) ? c : "";
        if (string.IsNullOrWhiteSpace(commitId))
        {
            progress.Report("Lỗi: Chưa cung cấp Commit ID cho Cherry-pick.");
            return AtomOutcome.Failed;
        }

        progress.Report($"\n>> [Cherry-Pick {commitId}]");
        
        var cmd = $"cherry-pick {commitId}";

        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, progress);

        if (exitCode != 0)
        {
            return AtomOutcome.Failed;
        }

        return AtomOutcome.Completed;
    }

    public async Task UndoAsync(WorkflowContext context, IProgress<string> progress)
    {
        // Undo a successful cherry pick usually means resetting HEAD by 1
        progress.Report("   Xoá commit vừa cherry-pick (reset --hard HEAD~1)...");
        await GitRunner.RunAsync("reset --hard HEAD~1", context.Config.WorkingDir, progress);
    }
}
