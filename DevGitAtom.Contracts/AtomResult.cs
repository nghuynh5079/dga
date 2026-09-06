namespace DevGitAtom.Contracts;

public record AtomResult(string AtomId, AtomOutcome Outcome, string? Message = null);

