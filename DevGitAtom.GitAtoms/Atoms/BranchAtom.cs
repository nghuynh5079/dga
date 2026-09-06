using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms;

public class BranchAtom : IAtom
{
    public string Id => "branch";
    public string DisplayName => "New Branch";
    public AtomCategory Category => AtomCategory.Navigation;

    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => [];

    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "branchName", DisplayName = "Tên nhánh mới", DefaultValue = "feature/new-branch" },
        new AtomParameter { Key = "checkout", DisplayName = "Checkout luôn (-b)", DefaultValue = "true" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        var branchName = context.CurrentParameters.TryGetValue("branchName", out var b) ? b : "feature/new-branch";
        var shouldCheckout = context.CurrentParameters.TryGetValue("checkout", out var co) && co.Equals("true", StringComparison.OrdinalIgnoreCase);

        progress.Report($"\n>> [Create Branch: {branchName}]");
        
        var cmd = shouldCheckout ? $"checkout -b {branchName}" : $"branch {branchName}";

        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, progress);

        if (exitCode != 0)
        {
            return AtomOutcome.Failed;
        }

        // Lưu lại để Undo biết đường mà xoá
        context.Data["BranchAtom_CreatedBranch"] = branchName;

        if (shouldCheckout)
            context.TargetBranch = branchName;

        return AtomOutcome.Completed;
    }

    public async Task UndoAsync(WorkflowContext context, IProgress<string> progress)
    {
        if (context.Data.TryGetValue("BranchAtom_CreatedBranch", out var b) && b is string branchName)
        {
            // Trở về nhánh base (nếu có) trước khi xoá
            var current = await GitRunner.GetCurrentBranchAsync(context.Config.WorkingDir);
            if (current == branchName)
            {
                var baseBranch = context.BaseBranch ?? "main";
                await GitRunner.RunAsync($"checkout {baseBranch}", context.Config.WorkingDir, progress);
            }
            
            progress.Report($"   Xóa nhánh vừa tạo: {branchName}");
            await GitRunner.RunAsync($"branch -D {branchName}", context.Config.WorkingDir, progress);
        }
    }
}
