namespace DevGitAtom.Core;

public record AtomResult(string AtomId, AtomOutcome Outcome, string? Message = null);
