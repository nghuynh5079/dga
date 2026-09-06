# Kiến trúc hệ thống DevGitAtom (DGA)

DevGitAtom được thiết kế theo kiến trúc **Node-based Workflow Engine**, giúp người dùng xây dựng các kịch bản thao tác Git thông qua việc ghép nối các hạt nhân (Atom).

## 1. Cấu trúc Project
- **DevGitAtom.Contracts**: Định nghĩa các giao diện cốt lõi (Interfaces) như `IAtom`, `WorkflowContext`, `AtomOutcome`, `AppConfig`. Dự án này không chứa logic thực thi, chỉ chứa định nghĩa.
- **DevGitAtom.Engine**: Trái tim của ứng dụng. Chứa `AtomInvoker` để chạy Chain, cơ chế Rollback (Transaction), Logging (như `JsonLogger`), và hệ thống `ChainValidator` kiểm tra ràng buộc Dependency Graph.
- **DevGitAtom.GitAtoms**: Chứa các class cụ thể thực thi giao diện `IAtom` (ví dụ: `PushAtom`, `CommitAtom`, `BranchAtom`). Đóng vai trò là thư viện các khối lệnh.
- **DevGitAtom.WPF**: Lớp giao diện người dùng (UI), tương tác với Engine. Bao gồm hệ thống Windows, Custom Controls, Themes, WebViews (nhúng xterm.js).

## 2. Luồng thực thi (Workflow Execution)
1. **Xây dựng chuỗi (Chain Builder)**: Người dùng chọn các Atom từ `AtomRegistry` và cấu hình tham số.
2. **Xác thực (Validation)**: `ChainValidator` sẽ phân tích `Requires` và `Provides` của từng Atom. Nếu một Atom yêu cầu điều kiện mà các Atom trước chưa đáp ứng (vd: `Push` cần `Commit`), Engine sẽ phát cảnh báo.
3. **Thực thi (Execution)**: `AtomInvoker` duyệt qua từng Atom.
4. **Giao dịch & Khôi phục (Transaction & Rollback)**: 
   - Nếu toàn bộ Atom chạy thành công, chuỗi kết thúc.
   - Nếu một Atom `Failed`, Engine tự động dừng và gọi `UndoAsync()` ngược từ dưới lên trên cho các Atom có cờ `Mutating = true`.
5. **Ghi log (Logging)**: Mọi thao tác, lỗi hoặc crash đều được ghi lại dưới định dạng JSONL vào thư mục `GitErrorLogs`.

## 3. Quản lý Theme & UI
- Hệ thống UI được chuẩn hoá thành các Resource Dictionary (`DarkTheme.xaml`, `LightTheme.xaml`).
- WebView2 được sử dụng để nhúng xterm.js nhằm hiển thị Terminal mô phỏng và Git Graph siêu mượt với khả năng tải trang động (Pagination).
