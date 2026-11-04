using System.Globalization;
using System.Text;
using fasterLegit.Domain.Advisor;

namespace fasterLegit.Services.Advisor;

public static class AdvisorKnowledge
{
    private static readonly AdvisorEvidence[] Entries =
    [
        new(
            "guide.memory",
            "Bộ nhớ / Memory",
            "RAM sử dụng cao trong nhiều mẫu là dấu hiệu áp lực bộ nhớ, chưa chứng minh nguyên nhân chậm. Kiểm tra tiến trình và đo lại cùng tải. Không hứa tăng tốc bằng xóa RAM hoặc tắt dịch vụ. Keywords: ram memory bo nho giat lag trinh duyet.",
            "https://learn.microsoft.com/en-us/windows/win32/perfctrs/about-performance-counters"
        ),
        new(
            "guide.startup",
            "Khởi động / Startup",
            "Mục khởi động bật không đồng nghĩa gây chậm. Xem vai trò và nhu cầu; giữ ứng dụng bảo mật, driver, sao lưu. Tắt khởi động không giải phóng RAM của tiến trình đang chạy và không đo được startup impact nếu chưa thu thập boot trace. Keywords: khoi dong startup boot bat may.",
            "https://support.microsoft.com/en-us/windows/experience/startup-boot/configure-startup-applications-in-windows"
        ),
        new(
            "guide.apps",
            "Ứng dụng / Applications",
            "Tên gói và nhãn Safe/Caution chỉ là quy tắc tham khảo. Ứng dụng có thể chứa dữ liệu cần giữ. Dependency khai báo trong manifest chỉ phản ánh quan hệ đã đọc được, không chứng minh gỡ an toàn. Không mặc định gỡ ứng dụng không rõ. Keywords: app ung dung go xoa bloatware dependency.",
            "https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.package.dependencies"
        ),
        new(
            "guide.storage",
            "Dung lượng / Storage",
            "Dung lượng trống thấp là dấu hiệu cần xem xét. Chỉ dọn nhóm tệp tạm đã định nghĩa và sau khi xác nhận; xóa không hoàn tác. Không gỡ Windows.old/Prefetch/Windows Update bằng lời khuyên tự sinh. Keywords: o dia dung luong day disk storage temp cache don dep.",
            "https://support.microsoft.com/en-us/windows/experience/storage/free-up-drive-space-in-windows"
        ),
        new(
            "guide.cpu",
            "Bộ xử lý / CPU",
            "CPU cao cần xét thời gian đo và ứng dụng đang sử dụng. Không suy ra lỗi phần cứng hay nhiệt độ từ % CPU; không đo được GPU/thermal thì nói chưa có dữ liệu. Keywords: cpu nong nhiet do processor game.",
            "https://learn.microsoft.com/en-us/windows/apps/develop/performance/"
        ),
        new(
            "guide.battery",
            "Pin / Battery",
            "Chưa thu thập báo cáo pin thì không kết luận độ chai hay ứng dụng gây hao pin. Hướng dẫn kiểm tra battery report; không dựng số liệu pin từ cấu hình RAM/CPU. Keywords: pin battery laptop sleep.",
            "https://support.microsoft.com/en-us/windows/experience/power-battery/caring-for-your-battery-in-windows"
        ),
    ];

    public static IReadOnlyList<AdvisorEvidence> Retrieve(string query)
    {
        var words = Normalize(query)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return Entries
            .Select(e =>
                (
                    Entry: e,
                    Score: words.Count(w =>
                        Normalize(e.Value + " " + e.Title).Contains(w, StringComparison.Ordinal)
                    )
                )
            )
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Id, StringComparer.Ordinal)
            .Take(3)
            .Select(x => x.Entry)
            .ToArray();
    }

    private static string Normalize(string value)
    {
        var result = new StringBuilder();
        foreach (var c in value.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                result.Append(c == 'đ' ? 'd' : c);
        return result.ToString().Normalize(NormalizationForm.FormC);
    }
}
