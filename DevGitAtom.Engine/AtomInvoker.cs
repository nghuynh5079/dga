using DevGitAtom.Contracts;
using DevGitAtom.Engine.Logging;
namespace DevGitAtom.Engine;

public class AtomInvoker(AtomRegistry registry)
{
    public async Task<AtomResult> InvokeAsync(
        ChainStep step,
        WorkflowContext context,
        IProgress<string> output)
    {
        var atom = registry.Get(step.AtomId);
        output.Report($"\n>> [{atom.DisplayName}]");
        
        context.LastAtomSkipped = false;
        context.CurrentParameters = step.Parameters;

        JsonLogger.LogInfo("Atom_Start", $"Executing atom {step.AtomId}", step.Parameters);

        int maxRetries = atom.Category == AtomCategory.Sync ? 2 : 0;
        int currentAttempt = 0;

        while (true)
        {
            try
            {
                var outcome = await atom.ExecuteAsync(context, output);
                if (context.LastAtomSkipped) outcome = AtomOutcome.Skipped;

                // Nếu thao tác mạng thất bại, thử lại
                if (outcome == AtomOutcome.Failed && currentAttempt < maxRetries)
                {
                    currentAttempt++;
                    output.Report($"   [RETRY] Có lỗi xảy ra, tự động thử lại lần {currentAttempt}/{maxRetries} sau 3 giây...");
                    await Task.Delay(3000);
                    continue;
                }

                var label = outcome switch
                {
                    AtomOutcome.Skipped => "[SKIP]",
                    AtomOutcome.Completed => "[OK]",
                    _ => "[FAIL]"
                };
                output.Report($"   {label} {outcome}");
                
                JsonLogger.LogInfo("Atom_End", $"Atom {step.AtomId} finished with outcome {outcome}");
                
                return new AtomResult(step.AtomId, outcome);
            }
            catch (Exception ex)
            {
                if (currentAttempt < maxRetries)
                {
                    currentAttempt++;
                    output.Report($"   [RETRY] Ngoại lệ '{ex.Message}', tự động thử lại lần {currentAttempt}/{maxRetries} sau 3 giây...");
                    await Task.Delay(3000);
                    continue;
                }

                output.Report($"   [FAIL] {ex.Message}");
                JsonLogger.LogError("Atom_Error", $"Atom {step.AtomId} failed", ex);
                return new AtomResult(step.AtomId, AtomOutcome.Failed, ex.Message);
            }
        }
    }

    public async Task<List<AtomResult>> RunChainAsync(
        IEnumerable<ChainStep> steps,
        WorkflowContext context,
        IProgress<string> output,
        CancellationToken ct = default)
    {
        var results = new List<AtomResult>();
        var executedAtoms = new Stack<IAtom>();
        
        var chainId = Guid.NewGuid().ToString("N");
        JsonLogger.LogInfo("Chain_Start", $"Starting chain {chainId} with {steps.Count()} steps.");
        
        bool chainFailed = false;

        foreach (var step in steps)
        {
            ct.ThrowIfCancellationRequested();
            var atom = registry.Get(step.AtomId);
            var result = await InvokeAsync(step, context, output);
            results.Add(result);
            
            if (result.Outcome == AtomOutcome.Completed || result.Outcome == AtomOutcome.Skipped)
            {
                executedAtoms.Push(atom);
                
                // FIX LỖ THỔNG 1: Không thể Rollback vượt biên thao tác mạng
                if (atom.Category == AtomCategory.Sync && result.Outcome == AtomOutcome.Completed)
                {
                    output.Report($"\n   [INFO] {atom.DisplayName} đã đẩy lên server. Xóa lịch sử Rollback các bước trước đó để tránh Desync!");
                    executedAtoms.Clear();
                }
            }
            else if (result.Outcome == AtomOutcome.Failed)
            {
                output.Report($"\n[CHAIN] Dừng lại vì atom '{step.AtomId}' Failed.");
                JsonLogger.LogError("Chain_Abort", $"Chain aborted at step {step.AtomId}");
                chainFailed = true;
                break;
            }
        }
        
        if (chainFailed && executedAtoms.Count > 0)
        {
            output.Report($"\n[ROLLBACK] Kích hoạt cơ chế Undo cho {executedAtoms.Count} atom trước đó...");
            JsonLogger.LogInfo("Chain_Rollback_Start", $"Starting rollback for chain {chainId}");
            
            while (executedAtoms.Count > 0)
            {
                var atomToUndo = executedAtoms.Pop();
                if (atomToUndo.Mutating)
                {
                    output.Report($"\n>> [Undo: {atomToUndo.DisplayName}]");
                    try
                    {
                        await atomToUndo.UndoAsync(context, output);
                        JsonLogger.LogInfo("Atom_Undo_Success", $"Undo completed for {atomToUndo.Id}");
                    }
                    catch (Exception ex)
                    {
                        // FIX LỖ THỔNG 2: Halt rollback nếu có exception
                        output.Report($"   [FATAL] DỪNG ROLLBACK: Gỡ '{atomToUndo.Id}' thất bại: {ex.Message}");
                        output.Report($"   Nguy cơ hỏng Working Tree! Các bước trước đó sẽ không được Undo tiếp.");
                        JsonLogger.LogError("Atom_Undo_Error", $"Undo failed for {atomToUndo.Id}", ex);
                        break;
                    }
                }
            }
            output.Report("\n[ROLLBACK] Đã kết thúc tiến trình Undo.");
        }
        
        JsonLogger.LogInfo("Chain_End", $"Chain {chainId} completed. (Failed: {chainFailed})");
        return results;
    }
}

