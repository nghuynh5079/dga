namespace DevGitAtom.Core;

public interface IAtom
{
    string Id { get; }
    string DisplayName { get; }
    AtomCategory Category { get; }
    bool Mutating { get; }
    string[] Requires { get; }
    string[] Provides { get; }
    Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output);
    Task UndoAsync(WorkflowContext context, IProgress<string> output) => Task.CompletedTask;
}
