using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms;

public class SimulateErrorAtom : IAtom
{
    public string Id => "git_simulate_error";
    public string DisplayName => "Simulate Git Error";
    public static string Description => "Giả lập các lỗi Git phổ biến để kiểm tra hệ thống báo lỗi.";
    public AtomCategory Category => AtomCategory.Diagnostic;
    
    public IEnumerable<AtomParameter> Parameters => [
        new AtomParameter { Key = "ErrorType", DisplayName = "ID Lỗi (1-20)", DefaultValue = "1" }
    ];

    public bool Mutating => false;
    public string[] Requires => [];
    public string[] Provides => [];

    public Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        string errorId = context.CurrentParameters.TryGetValue("ErrorType", out var v) ? v : "1";
        
        // Giả lập raw output của Git
        string rawError = errorId switch
        {
            "1" => "CONFLICT (content): Merge conflict in main.cs\nAutomatic merge failed; fix conflicts and then commit the result.",
            "2" => "error: Your local changes to the following files would be overwritten by checkout:\n  app.config\nPlease commit your changes or stash them before you switch branches.",
            "3" => "fatal: not a git repository (or any of the parent directories): .git",
            "4" => "To https://github.com/user/repo.git\n ! [rejected]        main -> main (non-fast-forward)\nerror: failed to push some refs to 'https://github.com/user/repo.git'\nhint: Updates were rejected because the tip of your current branch is behind",
            "5" => "fatal: A branch named 'feature-x' already exists.",
            "6" => "error: pathspec 'wrong_file.txt' did not match any file(s) known to git",
            "7" => "fatal: remote origin already exists.",
            "8" => "fatal: Authentication failed for 'https://github.com/user/repo.git/'",
            "9" => "git@github.com: Permission denied (publickey).\nfatal: Could not read from remote repository.",
            "10" => "ssh: Could not resolve hostname github.com: Name or service not known\nfatal: Could not read from remote repository.",
            "11" => "fatal: repository 'https://github.com/user/repo.git/' not found",
            "12" => "fatal: The current branch main has no upstream branch.\nTo push the current branch and set the remote as upstream, use\n    git push --set-upstream origin main",
            "13" => "error: you need to resolve your current index first\nsrc/main.cs: needs merge",
            "14" => "error: could not apply 1234abc... Add new feature",
            "15" => "fatal: bad revision 'wrong_commit_id'",
            "16" => "remote: error: GH001: Large files detected. You may want to try Git Large File Storage - https://git-lfs.github.com.\nremote: error: File big_data.zip is 150.00 MB; this exceeds GitHub's file size limit of 100.00 MB",
            "17" => "Note: switching to '1234abc'.\n\nYou are in 'detached HEAD' state. You can look around, make experimental\nchanges and commit them...",
            "18" => "fatal: refusing to merge unrelated histories",
            "19" => "error: Please commit your changes or stash them before you switch branches.\nAborting",
            "20" => "error: src refspec not_exist_branch does not match any\nerror: failed to push some refs to 'origin'",
            _ => "fatal: An unknown git error occurred."
        };

        // Dịch lỗi qua bộ Parser
        string translated = GitErrorParser.TranslateError(rawError);
        
        progress.Report($"\x1b[33m--- ĐANG CHẠY GIẢ LẬP LỖI SỐ {errorId} ---\x1b[0m");
        progress.Report(translated);

        return Task.FromResult(AtomOutcome.Failed);
    }
}
