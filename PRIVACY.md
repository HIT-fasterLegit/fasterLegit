# Quyền riêng tư của VELORA

Cập nhật: 05/11/2026. Nhóm phát triển bản sửa đổi: HIT Anyone'sLegend.
VELORA kế thừa optimizerDuck của fatDuck theo GPL v3; xem [ATTRIBUTION](ATTRIBUTION.md).

## Công cụ Windows và AI cục bộ

Các công cụ bảo trì thực hiện trên máy. Không bổ sung analytics hay gửi báo cáo
lỗi tự động. AI Ollama kết nối endpoint loopback do người dùng cấu hình; helper
cục bộ đặt runtime, model, profile và tệp tạm trong thư mục dự án. Cần Internet
khi tải runtime/model. Các công cụ tải tài nguyên theo yêu cầu vẫn có thể dùng
mạng. Không nên suy diễn rằng mọi chức năng của ứng dụng luôn ngoại tuyến.

## AI qua API là lựa chọn của người dùng

Khi chọn API, người dùng cấu hình endpoint/model/khóa riêng và phải đồng ý trước
khi phân tích. Câu hỏi, mục tiêu, ngữ cảnh hội thoại gần đây và bằng chứng được
gửi tới endpoint đó. Bằng chứng có thể gồm cấu hình Windows/phần cứng, CPU/RAM,
tên tiến trình, dung lượng ổ, tên/nhà phát hành startup, AppX và phụ thuộc đọc
được. Không gửi nội dung tài liệu cá nhân hay lệnh/đường dẫn startup thô. Không
nhập mật khẩu hoặc bí mật vào câu hỏi. Tên phần mềm và nội dung tự nhập vẫn có
thể nhạy cảm. Nhà cung cấp có thể nhìn thấy địa chỉ IP và giữ dữ liệu theo chính
sách riêng; VELORA không kiểm soát việc lưu trữ tại endpoint của người dùng.

Nút kiểm tra kết nối gửi lời chào kiểm tra, không thu thập bằng chứng hệ thống.
HTTPS được yêu cầu trừ loopback; chuyển hướng HTTP bị tắt. API có thể phát sinh
chi phí theo tài khoản người dùng. Không có endpoint hay khóa cloud dùng chung.

## Dữ liệu lưu trên máy

AI lưu settings.json và key.bin tại `UserData/Advisor` cạnh EXE. Khóa được bảo vệ
bằng Windows DPAPI theo tài khoản hiện tại và có thể xóa trong thiết lập; khóa
không nằm trong JSON hay báo cáo. Bảo vệ này không chống được phần mềm độc hại
chạy dưới cùng tài khoản. Hội thoại chỉ giữ trong bộ nhớ, có nút xóa.

Chỉ khi chọn lưu báo cáo, ứng dụng ghi JSON vào `UserData/Advisor/Reports`.
Báo cáo chứa diễn giải, bằng chứng, hành động, kết quả thực hiện và số đo trước/
sau nếu có; người dùng có thể xóa bằng File Explorer khi không cần. Không tự
đồng bộ hay tải báo cáo lên máy chủ. Kiểm tra nội dung trước khi chia sẻ.

Dữ liệu nền kế thừa như lịch sử thao tác, hoàn tác, log và thiết lập Windows vẫn
dùng đường dẫn dữ liệu đã có của ứng dụng; vị trí AI portable không di chuyển
toàn bộ dữ liệu cũ. `UserData`, model, runtime tải về và cache bị loại khỏi Git.
Để trao đổi về chính sách, liên hệ nhóm HIT Anyone'sLegend qua kho mã VELORA.
