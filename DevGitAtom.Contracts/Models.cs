namespace DevGitAtom.Contracts;

public class AtomParameter
{
    public string Key { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string DefaultValue { get; init; } = "";
}

public class ChainStep
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AtomId { get; set; } = "";
    public Dictionary<string, string> Parameters { get; set; } = new();
}


