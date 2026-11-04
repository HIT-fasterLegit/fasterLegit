# Trình diễn VELORA

## Chuẩn bị

Dùng máy Windows thử nghiệm có quyền quản trị viên. Sao lưu dữ liệu và tạo
điểm khôi phục. Build trước buổi trình diễn; không tải SDK hoặc package khi
thi. Kiểm tra tiếng Việt và cả hai chế độ sáng/tối.

## Câu chuyện

“VELORA kế thừa ý tưởng và mã nguồn mở optimizerDuck của fatDuck theo GPL v3.
Nhóm phát triển lại bộ nhận diện, bố cục và trải nghiệm sử dụng trên nền tảng đó,
giúp người dùng hiểu và chăm sóc Windows trong một không gian rõ ràng.”

Mở đầu phần thuyết trình bằng việc ghi nhận nguồn gốc. Phân biệt các chức năng
tối ưu, quản lý ứng dụng, khởi động, dọn dẹp và hoàn tác đã có với các phần nhóm
thực sự sửa hoặc bổ sung. Xem [ATTRIBUTION.md](../ATTRIBUTION.md).

## Hành trình khoảng năm phút

1. Tổng quan: giới thiệu logo V, điều hướng và dữ liệu phần cứng thật.
2. Tinh chỉnh: tìm một mục, đọc mô tả, giải thích rủi ro và hoàn tác.
3. Ứng dụng: tìm theo tên, giải thích bước chọn và xác nhận gỡ.
4. Khởi động: xem các mục; chỉ đổi mục thử nghiệm đã chuẩn bị.
5. Dung lượng: quét và xem tệp được chọn; giải thích xóa không thể hoàn tác.
6. Thiết lập: chuyển sáng/tối để thể hiện thiết kế thống nhất.

Không công bố tỷ lệ tăng tốc hoặc dung lượng thu hồi trước khi đo thực tế.
Không giới thiệu các ý tưởng chưa có mã nguồn/kiểm thử như tính năng đã hoàn thành.


## Demo AI của HIT Anyone'sLegend

1. Khởi động Ollama bằng local-ai.ps1 serve; model qwen3:4b đã tải trước khi demo.
2. Vào VELORA AI, kiểm tra kết nối cục bộ rồi hỏi: “Máy hay chậm khi mở nhiều
   ứng dụng, hãy đo CPU và RAM để xem cần kiểm tra gì trước.” Chờ lấy mẫu và xem
   bằng chứng gắn với nhận định, không hứa tăng tốc.
3. Đổi sang Tư vấn ứng dụng, yêu cầu giải thích AppX nào cần xem xét và phụ thuộc.
   Nêu rõ AI không biết mọi nhu cầu sử dụng, dữ liệu hay phụ thuộc.
4. Chọn Kế hoạch bảo trì với mục tiêu giải phóng dung lượng, giữ ứng dụng học tập.
   Xem hành động không được chọn sẵn. Chỉ chọn mở trang kiểm tra khi trình diễn
   trên máy thật; không cần gỡ/dọn hay tắt startup để chứng minh luồng xác nhận.
5. Đo lại và lưu báo cáo; giải thích so sánh quan sát không chứng minh nguyên nhân.
6. Mở Thiết lập AI để giới thiệu API tùy chọn. Không chiếu khóa API trên màn hình.

Giới thiệu đúng: nền tảng Windows kế thừa optimizerDuck/fatDuck (GPL v3), phần
AI và trải nghiệm VELORA do HIT Anyone'sLegend bổ sung. Chưa cấu hình provider
API thực thì chỉ trình bày khả năng tích hợp đã kiểm thử giao thức.
