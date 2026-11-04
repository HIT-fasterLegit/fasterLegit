<div align="center">

<img src="fasterLegit/Resources/Images/VeloraMark.svg" width="88" alt="VELORA" />

# VELORA

**Windows của bạn. Rõ ràng hơn.**

Không gian chăm sóc Windows của **HIT Anyone'sLegend**: chẩn đoán có dẫn chứng
bằng AI, xem xét ứng dụng và lập kế hoạch bảo trì do người dùng kiểm soát.

[Tính năng](#tính-năng) · [Chạy dự án](#chạy-dự-án) · [Kịch bản demo](fasterLegit/DEMO.md)

</div>

## Nguồn gốc và ghi nhận tác giả

**VELORA lấy ý tưởng và kế thừa mã nguồn từ [optimizerDuck](https://github.com/itsfatduck/optimizerDuck)
của [fatDuck (`itsfatduck`)](https://github.com/itsfatduck) cùng các tác giả đóng góp,
theo giấy phép GNU GPL v3.** Đây là một phiên bản phát triển phái sinh;
nhóm VELORA không tự xây dựng toàn bộ nền tảng từ đầu.

Các chức năng nền như tối ưu Windows, quản lý ứng dụng AppX, quản lý khởi động,
dọn dẹp ổ đĩa, ghi nhận thao tác và hoàn tác được kế thừa từ nền tảng mã nguồn
nhóm tiếp nhận. Phần nhóm VELORA cải tiến trong kho này gồm nhận diện/logo,
bố cục và điều hướng, nội dung giao diện, khả năng truy cập, tài liệu và quy trình
build/đóng gói cục bộ. Nhóm bổ sung AI Advisor: thu thập bằng chứng chỉ đọc,
tích hợp Ollama/API, lập kế hoạch có xác nhận và đo lại kết quả. Các phần sửa đổi
tiếp tục được phát hành theo GPL v3.

Xem [ghi nhận tác giả và phạm vi đóng góp](ATTRIBUTION.md),
[giấy phép](LICENSE) và [thông báo thư viện bên thứ ba](THIRD-PARTY-NOTICES.md).

![Giao diện VELORA](fasterLegit/DesignReview/Dark-1360-DashboardPage.png)

## Vì sao VELORA?

Mỗi thay đổi trên máy tính đều cần được hiểu rõ trước khi áp dụng. VELORA
tập hợp thông tin phần cứng và các công cụ bảo trì vào một giao diện thống
nhất. Người dùng xem mô tả, cân nhắc rủi ro và tự chọn thao tác; ứng dụng
ghi lại kết quả và lưu dữ liệu hoàn tác cho thay đổi có hỗ trợ khôi phục.

## Tính năng

| Không gian | Công việc |
| --- | --- |
| **VELORA AI** | Chẩn đoán triệu chứng từ số liệu thực, tư vấn AppX và lập kế hoạch bảo trì; hỗ trợ Ollama cục bộ hoặc API tương thích OpenAI |
| **Tổng quan** | Xem CPU, RAM, GPU, ổ đĩa, Windows, firmware, bảo mật và thời gian hoạt động |
| **Tinh chỉnh** | Xem xét các thay đổi trải nghiệm Windows và quyền riêng tư |
| **Thư viện ứng dụng** | Kiểm tra ứng dụng AppX đã cài và chọn ứng dụng muốn gỡ |
| **Quản lý khởi động** | Xem và bật/tắt các mục được hỗ trợ chạy cùng Windows |
| **Dọn dẹp ổ đĩa** | Quét, chọn và xóa tệp tạm; xem dung lượng trước khi thực hiện |

Giao diện tiếng Việt, hai chế độ sáng/tối, tìm kiếm, lọc và trạng thái tải,
trống, lỗi. Logo chữ V cùng bảng màu xanh ngọc tạo nhận diện xuyên suốt.

## VELORA AI

Ba chế độ: **Chẩn đoán**, **Tư vấn ứng dụng**, **Kế hoạch bảo trì**. AI chọn các
phép kiểm tra chỉ đọc trong danh sách cố định, nhận số liệu CPU/RAM/tiến trình,
dung lượng, startup và AppX khi phù hợp, rồi giải thích bằng các mã bằng chứng.
Hướng dẫn Microsoft được truy xuất từ bộ kiến thức nhỏ bằng từ khóa; nhóm không
tuyên bố tự huấn luyện model. Kết quả là tư vấn, không phải chứng nhận ứng dụng
an toàn để gỡ hay bằng chứng máy sẽ nhanh hơn.

Các hành động không được chọn sẵn. Anh/chị chọn từng mục và xác nhận trước khi
chạy. AI chỉ được đề xuất mở công cụ, tắt một số mục khởi động của người dùng,
gỡ AppX trong danh sách được phép hoặc dọn hai nhóm tệp tạm. Mã hành động được
kiểm tra lại và thao tác đi qua `OperationRunner`; AI không được chạy lệnh tùy ý.
Đo lại CPU/RAM và xuất báo cáo JSON giúp đối chiếu quan sát trước/sau, không chứng
minh quan hệ nhân quả. Xóa tệp và gỡ ứng dụng không có hoàn tác tự động.

### Chạy AI cục bộ

Windows x64; cần dung lượng cho runtime và model (riêng `qwen3:4b` khoảng 2,5 GB).
Chạy trong thư mục kho mã; helper đặt runtime, model, profile và cache ngay trong
`fasterLegit/.tools/ollama` trên ổ đĩa chứa dự án:

```powershell
.\fasterLegit\local-ai.ps1 install
.\fasterLegit\local-ai.ps1 serve
.\fasterLegit\local-ai.ps1 pull
.\fasterLegit\local-ai.ps1 status
```

Trong **VELORA AI → Thiết lập AI**, chọn Ollama, endpoint
`http://127.0.0.1:11434`, model `qwen3:4b`, lưu rồi kiểm tra kết nối.
Cần Internet khi tải runtime/model lần đầu. Suy luận cục bộ dùng model đã tải;
không hỗ trợ model Ollama có hậu tố cloud. Máy cấu hình yếu có thể phản hồi chậm.

### Dùng API

Chọn API tương thích OpenAI, nhập URL cơ sở (ví dụ endpoint của nhà cung cấp có
`/v1`), model và khóa của chính người dùng. API phải hỗ trợ Chat Completions và
JSON mode. Lưu cấu hình rồi kiểm tra kết nối. HTTPS được yêu cầu, trừ máy chủ
loopback. Khóa được bảo vệ bằng Windows DPAPI theo tài khoản hiện tại, không
ghi vào JSON cấu hình hay báo cáo. Không đưa khóa vào kho mã.

Trước khi phân tích qua API, người dùng phải đồng ý gửi câu hỏi, mục tiêu,
ngữ cảnh hội thoại và bằng chứng hệ thống cho endpoint đã chọn. Không nhập dữ
liệu bí mật; phí và chính sách dữ liệu phụ thuộc nhà cung cấp. Giao thức API đã
được kiểm thử bằng máy chủ giả lập; chưa xác minh với nhà cung cấp thực do chưa
cấu hình khóa. Ollama `qwen3:4b` đã chạy kiểm tra thực tế cả ba chế độ, chỉ đọc.

Xem [hướng dẫn và giới hạn AI](fasterLegit/Docs/AI_ADVISOR.md) và
[bản thuyết minh Word](fasterLegit/Docs/VELORA.docx).

## Cách sử dụng

1. Mở VELORA với quyền quản trị viên.
2. Xem **Tổng quan** để hiểu cấu hình và trạng thái máy.
3. Chọn công cụ, đọc mô tả và xem rủi ro của thao tác.
4. Chỉ chọn mục cần xử lý, sau đó xác nhận thực hiện.
5. Kiểm tra kết quả; dùng hoàn tác khi thao tác đó có hỗ trợ.

Xóa tệp và một số thao tác gỡ ứng dụng không thể hoàn tác. Sao lưu dữ liệu
quan trọng và tạo điểm khôi phục trước khi thay đổi hệ thống.

## Chạy dự án

Yêu cầu Windows 10/11 và .NET 10 SDK. Quy trình build đặt SDK, package,
cache và tệp tạm ngay trong dự án trên ổ đĩa hiện tại.

```powershell
.\fasterLegit\build.ps1 restore
.\fasterLegit\build.ps1 build
.\fasterLegit\build.ps1 run
```

Đóng gói bản Windows x64 chạy độc lập, không cần cài thêm .NET trên máy chạy:

```powershell
.\fasterLegit\build.ps1 publish
```

Mở `fasterLegit/Release/VELORA/VELORA.exe`. Giữ các tệp đi kèm trong cùng thư mục.

Nếu chưa có SDK, giải nén .NET 10 SDK cho Windows vào
`fasterLegit/.tools/dotnet`. Thư mục kỹ thuật của solution được giữ ổn định
để các lệnh build và dữ liệu hiện có tiếp tục tương thích.

Chạy kiểm thử từ terminal quản trị viên trên máy thử nghiệm:

```powershell
.\fasterLegit\build.ps1 test
```

## Tài liệu

- [Nguồn gốc, tác giả và phạm vi đóng góp](ATTRIBUTION.md)
- [Kịch bản trình diễn](fasterLegit/DEMO.md)
- [Hướng dẫn AI và giới hạn kiểm tra](fasterLegit/Docs/AI_ADVISOR.md)
- [Thuyết minh VELORA của HIT Anyone'sLegend](fasterLegit/Docs/VELORA.docx)
- [Quy chuẩn giao diện](DESIGN.md)
- [Luồng xử lý](fasterLegit/FLOW_VA_CO_CHE_TOI_UU_HOA.md)
- [Hướng dẫn phát triển](CONTRIBUTING.md)
- [Quyền riêng tư](PRIVACY.md), [Điều khoản](TERMS.md), [Lưu ý sử dụng](DISCLAIMER.md)

Giấy phép và thông báo bản quyền được lưu trong [LICENSE](LICENSE)
và [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
