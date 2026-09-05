namespace DevGitAtom.Core;

public class WorkflowContext
{
    public AppConfig Config { get; init; } = new();
    public string? OriginalBranch { get; set; }
    public string? BaseBranch { get; set; }
    public string? TargetBranch { get; set; }
    public string? StashRef { get; set; }
    public Dictionary<string, object?> Data { get; } = new();

    public bool LastAtomSkipped
    {
        get => Data.TryGetValue("__LastAtomSkipped", out var v) && v is true;
        set => Data["__LastAtomSkipped"] = value;
    }
}
