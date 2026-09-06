namespace DevGitAtom.Contracts;

public interface IAtom
{
    string Id { get; }
    string DisplayName { get; }
    AtomCategory Category { get; }
    bool Mutating { get; }
    string[] Requires { get; }
    string[] Provides { get; }
    IEnumerable<AtomParameter> Parameters => Array.Empty<AtomParameter>();
    
    Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> output);
    Task UndoAsync(WorkflowContext context, IProgress<string> output) => Task.CompletedTask;
}


