using System;
using System.Collections.Generic;
using DevGitAtom.Contracts;

namespace DevGitAtom.Engine;

public class ChainValidationResult
{
    public bool IsValid => Warnings.Count == 0;
    public List<string> Warnings { get; } = new();
}

public class ChainValidator
{
    private readonly AtomRegistry _registry;

    public ChainValidator(AtomRegistry registry)
    {
        _registry = registry;
    }

    public ChainValidationResult Validate(IEnumerable<ChainStep> steps)
    {
        var result = new ChainValidationResult();
        var providedTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Chúng ta giả định repo luôn ở trạng thái sạch hoặc đã có commit sẵn, 
        // nhưng để strict validation hoạt động, chúng ta tracking các token.
        
        int stepIndex = 1;
        foreach (var step in steps)
        {
            var atom = _registry.Get(step.AtomId);
            
            if (atom == null)
            {
                result.Warnings.Add($"Bước {stepIndex}: Atom '{step.AtomId}' không tồn tại trong hệ thống.");
                stepIndex++;
                continue;
            }

            // Check Requires
            foreach (var req in atom.Requires)
            {
                if (!providedTokens.Contains(req))
                {
                    result.Warnings.Add($"Bước {stepIndex} [{atom.DisplayName}]: Cần '{req}' nhưng chưa có Atom nào trước đó cung cấp. (Có thể chạy lỗi nếu Repo chưa sẵn sàng)");
                }
            }

            // Add Provides
            foreach (var prov in atom.Provides)
            {
                providedTokens.Add(prov);
            }

            stepIndex++;
        }

        return result;
    }
}
