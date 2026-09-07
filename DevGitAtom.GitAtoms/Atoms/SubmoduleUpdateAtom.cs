using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms.Atoms;

public class SubmoduleUpdateAtom : IAtom
{
    public string Id => "submodule_update";
    public string DisplayName => "Update Submodules";
    public AtomCategory Category => AtomCategory.Sync;
    public bool Mutating => false;
    public string[] Requires => [];
    public string[] Provides => [];
    
    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "mode", DisplayName = "Mode", DefaultValue = "update" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output)
    {
        var mode = context.CurrentParameters.TryGetValue("mode", out var m) ? m : "update";
        string args = mode switch
        {
            "init" => "submodule update --init",
            "sync" => "submodule sync",
            _ => "submodule update --init --recursive"
        };
        
        var (exitCode, stdout) = await GitRunner.RunAsync(args, context.Config.WorkingDir, output);
        
        if (exitCode == 0 && string.IsNullOrWhiteSpace(stdout))
        {
            output.Report("Skipped: No submodule updates.");
            return AtomOutcome.Skipped;
        }
        
        return exitCode == 0 ? AtomOutcome.Completed : AtomOutcome.Failed;
    }
}
