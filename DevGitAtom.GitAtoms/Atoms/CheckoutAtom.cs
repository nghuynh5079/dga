using DevGitAtom.Contracts;
namespace DevGitAtom.GitAtoms;

public class CheckoutAtom : IAtom
{
    public string Id => "checkout";
    public string DisplayName => "Checkout Branch";
    public AtomCategory Category => AtomCategory.Worktree;
    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => [];
    
    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "branch", DisplayName = "Branch Name", DefaultValue = "main" },
        new AtomParameter { Key = "create", DisplayName = "Tạo nhánh mới? (true/false)", DefaultValue = "false" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        var branch = context.CurrentParameters.TryGetValue("branch", out var b) ? b : "main";
        var createStr = context.CurrentParameters.TryGetValue("create", out var c) ? c : "false";
        var isCreate = createStr.Equals("true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(branch))
        {
            output.Report("Lỗi: Branch name tr\u1ed1ng.");
            return AtomOutcome.Failed;
        }

        // LƯU LẠI NHÁNH CŨ VÀO CONTEXT TRƯỚC KHI CHECKOUT (Phục vụ Rollback)
        var currentBranch = await GitRunner.GetCurrentBranchAsync(context.Config.WorkingDir);
        context.Data["Checkout_PrevBranch"] = currentBranch;

        var flag = isCreate ? "-b " : "";
        var (exitCode, _) = await GitRunner.RunAsync($"checkout {flag}{branch}", context.Config.WorkingDir, output);
        
        if (exitCode == 0)
        {
            context.TargetBranch = branch;
            return AtomOutcome.Completed;
        }
        return AtomOutcome.Failed;
    }

    public async Task UndoAsync(WorkflowContext context, IProgress<string> output)
    {
        if (context.Data.TryGetValue("Checkout_PrevBranch", out var prev) && prev is string prevBranch && !string.IsNullOrWhiteSpace(prevBranch))
        {
            output.Report($"   Khôi phục nhánh cũ: '{prevBranch}'...");
            await GitRunner.RunAsync($"checkout {prevBranch}", context.Config.WorkingDir, output);
        }
    }
}



