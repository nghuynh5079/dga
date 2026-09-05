namespace DevGitAtom.Core;

public class AtomInvoker(AtomRegistry registry)
{
    public async Task<AtomResult> InvokeAsync(
        string atomId,
        WorkflowContext context,
        IProgress<string> output)
    {
        var atom = registry.Get(atomId);
        output.Report($"\n>> [{atom.DisplayName}]");
        context.LastAtomSkipped = false;

        try
        {
            var outcome = await atom.ExecuteAsync(context, output);
            if (context.LastAtomSkipped) outcome = AtomOutcome.Skipped;

            var label = outcome switch
            {
                AtomOutcome.Skipped => "[SKIP]",
                AtomOutcome.Completed => "[OK]",
                _ => "[?]"
            };
            output.Report($"   {label} {outcome}");
            return new AtomResult(atomId, outcome);
        }
        catch (Exception ex)
        {
            output.Report($"   [FAIL] {ex.Message}");
            return new AtomResult(atomId, AtomOutcome.Failed, ex.Message);
        }
    }

    public async Task<List<AtomResult>> RunChainAsync(
        IEnumerable<string> atomIds,
        WorkflowContext context,
        IProgress<string> output,
        CancellationToken ct = default)
    {
        var results = new List<AtomResult>();
        foreach (var id in atomIds)
        {
            ct.ThrowIfCancellationRequested();
            var result = await InvokeAsync(id, context, output);
            results.Add(result);
            if (result.Outcome == AtomOutcome.Failed)
            {
                output.Report($"\nChain dung lai vi atom '{id}' Failed.");
                break;
            }
        }
        return results;
    }
}