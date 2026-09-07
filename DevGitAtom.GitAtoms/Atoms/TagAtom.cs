using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevGitAtom.Contracts;

namespace DevGitAtom.GitAtoms.Atoms;

public class TagAtom : IAtom
{
    public string Id => "tag";
    public string DisplayName => "Tag";
    public AtomCategory Category => AtomCategory.History;
    public bool Mutating => true;
    public string[] Requires => [];
    public string[] Provides => [];

    public IEnumerable<AtomParameter> Parameters => new[]
    {
        new AtomParameter { Key = "tagName", DisplayName = "Tên thẻ (Tag Name)", DefaultValue = "v1.0.0" },
        new AtomParameter { Key = "message", DisplayName = "Thông điệp (-m)", DefaultValue = "Release v1.0.0" }
    };

    public async Task<AtomOutcome> ExecuteAsync(WorkflowContext context, IProgress<string> progress)
    {
        var tagName = context.CurrentParameters.TryGetValue("tagName", out var t) && !string.IsNullOrWhiteSpace(t) ? t : "v1.0.0";
        var message = context.CurrentParameters.TryGetValue("message", out var m) ? m : "";

        progress.Report($"\n>> [Create Tag: {tagName}]");
        
        var msgFlag = !string.IsNullOrWhiteSpace(message) ? $" -m \"{message}\"" : "";
        var cmd = $"tag -a {tagName}{msgFlag}";

        var (exitCode, _) = await GitRunner.RunAsync(cmd, context.Config.WorkingDir, progress);

        if (exitCode != 0)
        {
            return AtomOutcome.Failed;
        }

        context.Data["TagAtom_CreatedTag"] = tagName;
        return AtomOutcome.Completed;
    }

    public async Task UndoAsync(WorkflowContext context, IProgress<string> progress)
    {
        if (context.Data.TryGetValue("TagAtom_CreatedTag", out var t) && t is string tagName)
        {
            progress.Report($"   Xóa tag vừa tạo: {tagName}");
            await GitRunner.RunAsync($"tag -d {tagName}", context.Config.WorkingDir, progress);
        }
    }
}
