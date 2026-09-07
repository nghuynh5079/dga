using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms.Atoms;

public class BranchProtectionAtom : IAtom
{
    public string Id => "branch_protection";
    public string DisplayName => "Assert Branch Protection";
    public AtomCategory Category => AtomCategory.Safety;
    public bool Mutating => false;
    public string[] Requires => [];
    public string[] Provides => ["BranchProtectionChecked"];
    
    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "protectedBranches", DisplayName = "Protected Branches", DefaultValue = "main,master,develop,release" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        var pb = context.CurrentParameters.TryGetValue("protectedBranches", out var val) ? val : "main,master,develop,release";
        var protectedList = pb.Split(',').Select(x => x.Trim()).ToList();
        
        var currentBranch = await GitRunner.GetCurrentBranchAsync(context.Config.WorkingDir);
        
        if (protectedList.Contains(currentBranch))
        {
            throw new Exception($"Branch '{currentBranch}' is protected. Cannot perform this operation.");
        }
        
        output.Report($"Branch '{currentBranch}' is not protected. OK.");
        return AtomOutcome.Completed;
    }
}
