# Nguồn gốc và ghi nhận tác giả của VELORA

Cập nhật: 05/11/2026.

## Dự án nền tảng

VELORA lấy ý tưởng và kế thừa mã nguồn từ **optimizerDuck**, dự án tối ưu và
chăm sóc Windows của **fatDuck (`itsfatduck`)** cùng các tác giả đóng góp.
VELORA là phiên bản phát triển phái sinh, không phải sản phẩm được nhóm viết
toàn bộ từ đầu. Tên VELORA dùng để phân biệt bản sửa đổi này với dự án gốc;
việc sử dụng mã nguồn không đồng nghĩa với việc tác giả gốc bảo trợ bản sửa đổi.

- [Dự án gốc optimizerDuck](https://github.com/itsfatduck/optimizerDuck)
- [Hồ sơ tác giả fatDuck](https://github.com/itsfatduck)
- [Giấy phép dự án gốc](https://github.com/itsfatduck/optimizerDuck/blob/master/LICENSE)
- [Kho mã nguồn VELORA](https://github.com/HIT-fasterLegit/fasterLegit)
- Giấy phép đi kèm bản này: [LICENSE](LICENSE), GNU GPL phiên bản 3.

Bản mã nhóm tiếp nhận sử dụng tên kỹ thuật `fasterLegit`. Các đường dẫn dự án,
namespace và định danh dữ liệu tương ứng được giữ để bảo đảm tương thích.
Commit `8f32585` là bản mã được nhập vào kho này, không phải bằng chứng rằng
nhóm là tác giả ban đầu của toàn bộ nội dung. Kho này không xác nhận commit đó
trùng hoàn toàn với một commit hoặc bản phát hành cụ thể của optimizerDuck.

## Phân biệt phần kế thừa và phần cải tiến

| Phạm vi | Nguồn đóng góp |
| --- | --- |
| Nền tảng C#/.NET/WPF, tổ chức dịch vụ và luồng xử lý hệ thống | Kế thừa từ nền tảng mã nguồn tiếp nhận |
| Các chức năng tối ưu, quản lý AppX, khởi động, dọn dẹp và khả năng hoàn tác đã có | Kế thừa; không ghi nhận là chức năng do nhóm VELORA sáng tạo từ đầu |
| Nhận diện VELORA, logo và bảng màu sáng/tối | Phần cải tiến trong kho VELORA |
| Điều hướng ngang; bố cục tổng quan, tinh chỉnh, ứng dụng, khởi động, dọn dẹp và thiết lập | Phần cải tiến trong kho VELORA |
| Nội dung giao diện, nhãn truy cập cho các điều khiển đã sửa và kiểm thử liên quan | Phần cải tiến trong kho VELORA; cơ chế bản địa hóa và kiểm thử nền được kế thừa |
| Tài liệu VELORA, kịch bản demo, cấu hình build/cache cục bộ và nhận diện bản đóng gói | Phần cải tiến trong kho VELORA |

Các ý tưởng phát triển tiếp, bao gồm AI Advisor, không được xem là tính năng
đã triển khai hoặc đóng góp đã chứng minh chỉ vì được nêu trong bản thuyết minh.
Khi bổ sung chức năng, cần cập nhật bảng này dựa trên mã nguồn và kiểm thử thực tế.

## Quyền tác giả và giấy phép

- Mã nguồn gốc: **Copyright (C) 2025–2026 fatDuck (itsfatduck)**;
  giữ nguyên các thông báo khác của những tác giả và thư viện liên quan.
- Phần sửa đổi riêng trong kho VELORA: **Copyright (C) 2026 VELORA contributors**.
  Ghi nhận này chỉ áp dụng cho đóng góp riêng, không chuyển quyền sở hữu mã gốc.
- Bản sửa đổi tiếp tục được cung cấp theo GNU GPL v3; không bổ sung điều kiện
  tước bỏ các quyền mà giấy phép cấp cho người nhận.
- Khi phân phối bản EXE, cung cấp giấy phép, thông báo tác giả và mã nguồn tương
  ứng với bản phân phối theo điều kiện GPL v3. Kho mã mới nhất không tự bảo đảm
  tương ứng với mọi bản EXE; cần xác định commit/tag dùng để build bản đó.

Các thông báo thư viện bên thứ ba được giữ tại
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Cách giới thiệu khi trình bày sản phẩm

“VELORA được phát triển dựa trên ý tưởng và mã nguồn mở optimizerDuck của
fatDuck theo giấy phép GPL v3. Nhóm kế thừa nền tảng xử lý Windows và tập trung
cải tiến nhận diện, trải nghiệm sử dụng, bố cục giao diện cùng các phần bổ sung
được trình bày rõ trong tài liệu đóng góp.”

Ngày 05/11/2026, nhóm cập nhật tài liệu ghi nhận nguồn gốc, thông báo tác giả
và metadata bản đóng gói. Việc ghi nhận không thay thế điều kiện riêng của
cuộc thi; nhóm cần trình bày đúng phạm vi đóng góp khi nộp hồ sơ.
