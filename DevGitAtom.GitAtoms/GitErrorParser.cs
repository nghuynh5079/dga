using System.Text.RegularExpressions;

namespace DevGitAtom.GitAtoms;

public static class GitErrorParser
{
    private static readonly Dictionary<string, string> ErrorDictionary = new()
    {
        { @"CONFLICT \(content\): Merge conflict in (.*)", "🔴 XUNG ĐỘT (MERGE CONFLICT): File '$1' đang có xung đột. Hãy mở file này lên, sửa phần bị đánh dấu <<<<<< ====== >>>>>>, sau đó 'git add' và 'git commit' lại." },
        { @"Your local changes to the following files would be overwritten by (checkout|merge):", "🟡 CHƯA LƯU THAY ĐỔI: Bạn đang có file sửa dở chưa được commit. Nếu tiếp tục ($1), code mới sẽ đè mất code bạn vừa viết. Hãy 'git commit' hoặc 'git stash' trước nhé." },
        { @"fatal: not a git repository", "🔴 KHÔNG PHẢI REPO: Thư mục này chưa được khởi tạo Git. Hãy chọn đúng thư mục chứa code hoặc chạy 'git init' trước." },
        { @"Updates were rejected because the tip of your current branch is behind", "🟠 CHƯA CẬP NHẬT (NON-FAST-FORWARD): Code trên mạng (remote) đang mới hơn code ở máy bạn. Bạn không thể Push đè lên được. Hãy bấm 'Pull' để kéo code mới về trước, giải quyết xung đột (nếu có), rồi mới 'Push' lên nhé." },
        { @"fatal: A branch named '(.*)' already exists.", "🟡 TRÙNG TÊN NHÁNH: Không thể tạo nhánh '$1' vì nó đã tồn tại rồi. Hãy thử tên khác hoặc dùng Checkout để chuyển sang nhánh đó." },
        { @"error: pathspec '(.*)' did not match any file\(s\) known to git", "🔴 SAI ĐƯỜNG DẪN: Không tìm thấy file hoặc thư mục có tên '$1'. Chắc chắn rằng bạn gõ đúng tên hoặc file thực sự tồn tại nhé." },
        { @"fatal: remote origin already exists.", "🟡 ĐÃ CÓ REMOTE: Remote có tên 'origin' đã tồn tại rồi, không thể thêm mới. Nếu muốn đổi đường dẫn, hãy dùng lệnh 'git remote set-url origin <url_mới>'." },
        { @"fatal: Authentication failed for '(.*)'", "🔴 LỖI XÁC THỰC: Sai tài khoản hoặc mật khẩu (Personal Access Token) khi kết nối tới '$1'. Kiểm tra lại thông tin đăng nhập của bạn." },
        { @"Permission denied \(publickey\)", "🔴 LỖI SSH KEY: Git Server (GitHub/GitLab) từ chối quyền truy cập vì không nhận diện được SSH Key của bạn. Kiểm tra lại xem bạn đã nạp đúng SSH Key chưa." },
        { @"ssh: Could not resolve hostname", "🔴 LỖI MẠNG / SAI TÊN MIỀN: Không thể kết nối tới máy chủ. Hãy kiểm tra lại kết nối mạng hoặc xem link repository có gõ sai không." },
        { @"fatal: repository '(.*)' not found", "🔴 KHÔNG TÌM THẤY REPO: Repository '$1' không tồn tại hoặc bạn chưa được cấp quyền (Invite) để truy cập nó." },
        { @"fatal: The current branch (.*) has no upstream branch.", "🟡 CHƯA LINK REMOTE (NO UPSTREAM): Nhánh '$1' của bạn mới chỉ có ở dưới máy, chưa có trên mạng. Lần Push đầu tiên hãy cấu hình bằng lệnh 'git push --set-upstream origin $1'." },
        { @"error: you need to resolve your current index first", "🟠 CHƯA XỬ LÝ CONFLICT XONG: Bạn đang bị kẹt ở trạng thái Merge/Rebase do xung đột. Hãy sửa file lỗi, 'git add', rồi chạy tiếp lệnh." },
        { @"error: could not apply (.*)", "🔴 LỖI CHERRY-PICK/REVERT: Không thể áp dụng thay đổi ở commit '$1' vì có xung đột (Conflict). Hãy mở file ra sửa thủ công nhé." },
        { @"fatal: bad revision '(.*)'", "🔴 SAI COMMIT ID: Commit hoặc Branch tên '$1' không tồn tại. Hãy kiểm tra lại mã SHA hoặc tên nhánh." },
        { @"GH001: Large files detected", "🔴 LỖI FILE QUÁ TO (GH001): Bạn đang cố Push một file lớn hơn giới hạn của GitHub (thường là > 100MB). Hãy xóa file đó khỏi lịch sử commit hoặc dùng Git LFS." },
        { @"You are in 'detached HEAD' state.", "🟡 CẢNH BÁO DETACHED HEAD: Bạn đang ở trạng thái 'rời rạc', không gắn với nhánh nào (có thể do vừa checkout một commit cũ). Mọi thay đổi ở đây sẽ bị mất nếu chuyển nhánh khác. Hãy tạo nhánh mới nếu muốn lưu lại: 'git checkout -b <tên_nhánh>'." },
        { @"fatal: refusing to merge unrelated histories", "🔴 LỊCH SỬ KHÁC BIỆT: Hai nhánh bạn đang ghép không có chung nguồn gốc (Unrelated histories). Nếu cố tình muốn ghép, bạn phải dùng cờ '--allow-unrelated-histories'." },
        { @"Please commit your changes or stash them before you switch branches", "🟡 KHÔNG THỂ CHUYỂN NHÁNH: Chuyển nhánh sẽ làm mất các code bạn vừa viết nhưng chưa lưu. Hãy 'git commit' hoặc cất tạm bằng 'git stash' trước nhé." },
        { @"error: src refspec (.*) does not match any", "🔴 LỖI SAI TÊN NHÁNH PUSH: Bạn đang cố push nhánh '$1' nhưng nhánh này không tồn tại ở dưới máy. Có thể bạn gõ sai tên hoặc chưa tạo nhánh." }
    };

    public static string TranslateError(string rawOutput)
    {
        var result = rawOutput;
        foreach (var kvp in ErrorDictionary)
        {
            var match = Regex.Match(rawOutput, kvp.Key, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success)
            {
                var replacement = kvp.Value;
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    replacement = replacement.Replace($"${i}", match.Groups[i].Value);
                }
                
                // Append the friendly error to the beginning of the raw output
                return $"\x1b[1;31m[TRỢ LÝ GIT]\x1b[0m {replacement}\n\n[Log chi tiết của Git]:\n{rawOutput}";
            }
        }
        return rawOutput;
    }
}
