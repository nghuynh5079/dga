# DevGitAtom UI Design System

Tài liệu này định nghĩa các quy chuẩn chung về giao diện cho toàn bộ dự án `DevGitAtom`. Mọi thành phần UI được tạo ra (Popups, UserControls, Windows mới) đều nên tuân theo.

## 1. File cấu hình gốc
Toàn bộ mã màu và style chung được định nghĩa tại `DevGitAtom.WPF/Themes/DarkTheme.xaml`.
File này đã được đăng ký global trong `App.xaml`.

## 2. Bảng Màu (Color Palette)
- **BgPrimary** (`#0d1117`): Màu nền mặc định của app, dùng cho Grid/Window chính.
- **BgSecondary** (`#161b22`): Màu của các Panel con, Sidebar, Header.
- **BgTertiary** (`#21262d`): Màu của các Item, Card, Buttons.
- **BorderColor** (`#30363d`): Màu chung cho tất cả các đường viền (Border/Splitter).
- **TextPrimary** (`#e6edf3`): Chữ chính.
- **TextMuted** (`#8b949e`): Chữ phụ, hướng dẫn, title mờ.
- **AccentBlue** (`#58a6ff`): Màu nhấn cho thao tác chính.
- **AccentGreen** (`#3fb950`): Màu nhấn thành công.
- **AccentRed** (`#ff7b72`): Màu báo lỗi / Xóa.

*Cách dùng trong XAML:* `Background="{StaticResource BgSecondary}"`

## 3. Các Style Tiêu Chuẩn

### Text & Controls cơ bản
- `TextBox`, `TextBlock`, `Label`: Đã được overide mặc định. Cứ ném thẻ `<TextBox />` vào là tự động có viền bo góc, màu dark mode chuẩn chỉnh, hover xanh dương.

### Buttons
Bắt buộc dùng một trong 3 styles sau thay vì `<Button/>` rỗng:
1. `Style="{StaticResource AtomButton}"`: Nút bấm dạng thẻ vuông, viền xám, hover hiện viền xanh. Dùng làm nút phụ, chức năng.
2. `Style="{StaticResource RunButton}"`: Nút màu xanh lá (dành riêng cho các action thực thi chính).
3. `Style="{StaticResource DangerButton}"`: Nút chữ đỏ, hover nền đỏ sẫm. Dùng cho Xóa, Hủy.

### Containers
1. `Style="{StaticResource ChainItem}"`: Style dành cho Border của 1 item list kéo thả.
2. `Style="{StaticResource PanelBorder}"`: Dành cho các hộp/panel bo tròn góc 8px.

## 4. Notifications (Toast / Popup)
Thay vì dùng `MessageBox.Show()` cồng kềnh, Windows XP, hãy gọi hàm Notification tích hợp sẵn.
Hàm này bung ra một cái Toast màu đen, bóng mờ ở giữa đáy màn hình tự tắt sau 3 giây.

*Từ MainWindow:*
```csharp
// Thông báo thường
ShowNotification("Lưu cấu hình thành công!");

// Thông báo lỗi
ShowNotification("Đã có lỗi xảy ra trong lúc chạy!", isError: true);
```

Nếu đang ở UserControl hoặc Dialog khác, có thể truy xuất bằng cách:
```csharp
((MainWindow)Application.Current.MainWindow).ShowNotification("Done!");
```
