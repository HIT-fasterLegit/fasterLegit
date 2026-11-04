# Phát triển VELORA

VELORA kế thừa ý tưởng và mã nguồn của [optimizerDuck](https://github.com/itsfatduck/optimizerDuck)
do fatDuck (`itsfatduck`) và cộng đồng phát triển, theo GPL v3. Đọc
[ATTRIBUTION.md](ATTRIBUTION.md) để phân biệt phần nền kế thừa và phần cải tiến.

Windows, .NET 10 SDK, WPF và WPF-UI. Đọc AGENTS.md trước khi sửa mã.
Dùng `fasterLegit/build.ps1` để giữ SDK, package, cache và tệp tạm trong dự án.

## Nguyên tắc

- Mọi thao tác thay đổi Windows đi qua OperationRunner.
- Nội dung hiển thị phải dùng tài nguyên dịch; cập nhật bản tiếng Việt.
- Dùng token và style chung trong UI/Styles/FluentDesign.xaml.
- Kiểm tra sáng/tối, cửa sổ tối thiểu và nội dung dài.
- Giữ điều hướng bàn phím, trạng thái tải, trống và lỗi.
- Giữ khóa dữ liệu và định danh thao tác ổn định khi đổi thương hiệu.
- Giữ giấy phép và thông báo bản quyền bắt buộc.
- Ghi rõ phần kế thừa và phần thay đổi; bổ sung thông báo sửa đổi và ngày tương
  ứng khi cần. Không ghi nhận tính năng sẵn có của optimizerDuck là sáng tạo mới
  của nhóm VELORA.

## Xác minh

Build Release không warning. Chạy kiểm thử phù hợp trên máy thử nghiệm;
các test registry, service hoặc task cần quyền quản trị viên.
Kèm ảnh khi sửa bố cục; mô tả hành vi và giới hạn thực tế.
