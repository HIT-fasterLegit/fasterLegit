# VELORA AI · HIT Anyone'sLegend

VELORA kế thừa nền tảng Windows từ optimizerDuck của fatDuck theo GPL v3.
Phần AI Advisor là bổ sung của nhóm; xem [ATTRIBUTION](../../ATTRIBUTION.md).

## Luồng sử dụng

1. Mở VELORA AI hoặc liên kết AI trong Thiết lập. Mở Thiết lập AI, chọn Ollama
   hoặc API tương thích OpenAI; lưu mọi thay đổi trước khi phân tích.
2. Chọn Chẩn đoán, Tư vấn ứng dụng hoặc Kế hoạch bảo trì. Mô tả triệu chứng và
   mục tiêu cụ thể. Nếu dùng API, đọc và chọn đồng ý gửi dữ liệu.
3. Chọn Phân tích. Có thể hủy yêu cầu đang chạy. Câu hỏi quá ngắn có thể cần
   làm rõ; lỗi kết nối, cấu hình hay JSON không hợp lệ được hiển thị.
4. Đọc tóm tắt, nhận định và mở Bằng chứng để kiểm tra mã, giá trị và nguồn.
5. Chọn từng hành động rồi xác nhận danh sách. Không có mục nào được chọn sẵn.
   Có thể chỉ mở trang công cụ để tự xem xét, không bắt buộc thay đổi hệ thống.
6. Nếu đã có phép đo ban đầu, chọn Đo lại rồi lưu báo cáo JSON khi cần.

## Cơ chế và giới hạn

AI lập kế hoạch chỉ từ bốn phép kiểm tra: tài nguyên, ổ đĩa, startup và AppX.
Ứng dụng bổ sung phép kiểm tra bắt buộc theo chế độ. CPU/RAM được lấy mẫu mỗi
giây trong khoảng cấu hình 5–60 giây; danh sách tiến trình có giới hạn. Đây không
phải đo toàn bộ hiệu năng, thời gian boot, nhiệt độ, GPU hay I/O ổ đĩa.

Tư vấn ứng dụng chỉ xem các AppX có thể gỡ và manifest đọc được, có giới hạn số
lượng. Quan hệ phụ thuộc không đầy đủ; không biết dữ liệu chưa sao lưu, thói quen
sử dụng hoặc mọi phụ thuộc của phần mềm. Không được coi nhãn hay lời AI là bảo
đảm gỡ an toàn. Gỡ tự động chỉ được tạo cho ứng dụng trong danh sách cho phép
có sẵn, không có phụ thuộc đã phát hiện. Không gỡ provisioned package cho các
tài khoản tương lai qua AI. Các ứng dụng khác chỉ được tư vấn hoặc mở trang xem.

Kho hướng dẫn nhỏ được viết riêng, dẫn tài liệu Microsoft và truy xuất bằng từ
khóa. Đây không phải mô hình học từ CSV hay model do nhóm huấn luyện. LLM có thể
diễn giải sai dù mã bằng chứng hợp lệ; người dùng cần đọc bằng chứng gốc.

AI không được tạo shell command/PowerShell hay tự sửa Registry. Danh sách hành
động cố định gồm mở trang, tắt startup người dùng được hỗ trợ, gỡ AppX đủ điều
kiện và dọn TempFiles/SystemTemp. Báo cáo hành động hết hạn sau 30 phút; đích
được kiểm tra lại ngay trước khi chạy. Thay đổi dùng dịch vụ Windows kế thừa
qua OperationRunner, có kết quả từng mục. Hủy không hoàn tác công việc đã xong.
Startup có thể bật lại thủ công; AI không cung cấp tự động undo cho xóa/gỡ.

So sánh trước/sau chỉ mô tả hai lần quan sát; tải nền và chính model có thể làm
thay đổi số liệu. Ollama được yêu cầu dỡ model sau trả lời (`keep_alive=0`).
Không gán phần trăm tăng tốc hoặc khẳng định thay đổi là nguyên nhân cải thiện.

## Cấu hình và dữ liệu

Xem [README](../../README.md#velora-ai) để cài Ollama trong thư mục dự án.
Mặc định `qwen3:4b`, native `/api/chat`, JSON schema, context 8192. Helper tải
Ollama phiên bản ghim với SHA-256 kiểm tra; model tải riêng, không nằm trong Git
hay gói EXE. Runtime/model có giấy phép riêng trong thông báo bên thứ ba.

API sử dụng `/chat/completions` nối vào URL cơ sở, bearer key và JSON mode.
Endpoint/model do người dùng chọn, không kèm tài khoản hay khóa dùng chung.
Khóa DPAPI phụ thuộc tài khoản Windows, không có tính di động như file cấu hình.
Đổi nhà cung cấp/cấu hình yêu cầu lưu và đồng ý gửi dữ liệu lại.

Thiết lập và khóa ở `UserData/Advisor` bên cạnh EXE, báo cáo được lưu chủ động
ở `UserData/Advisor/Reports`. Hội thoại chỉ giữ trong bộ nhớ và có nút xóa.
Báo cáo có câu diễn giải, nguồn/bằng chứng, danh sách hành động và số đo/kết quả,
không chứa khóa API. Hội thoại và dữ liệu hệ thống có thể nhạy cảm; chia sẻ báo
cáo sau khi tự kiểm tra. UserData được bỏ qua bởi Git.

Chế độ local chỉ cho loopback, từ chối URL có credential/query/fragment và model
cloud. API yêu cầu đồng ý trước khi thu thập/gửi. Thông tin tên ứng dụng và tiến
trình có thể lộ phần mềm đang dùng; nội dung người dùng tự nhập được gửi nguyên
ngữ nghĩa. Không đọc nội dung tài liệu cá nhân. Xem [quyền riêng tư](../../PRIVACY.md).

## Kiểm chứng

Kiểm thử bao gồm consent, URL/model validation, DPAPI, giao thức hai provider,
JSON sai, mã hành động/bằng chứng bịa, báo cáo hết hạn và XML manifest có entity
ngoài. Smoke test bằng Ollama qwen3:4b thực tế đã tạo báo cáo có bằng chứng cho
cả ba chế độ. Không thực hiện gỡ/dọn/tắt startup trên máy cá nhân để chạy demo.
API thực cần được kiểm tra sau khi người dùng tự cấu hình nhà cung cấp và khóa.
