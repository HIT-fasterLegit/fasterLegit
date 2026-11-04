using System.IO;
using System.Text.Json;
using fasterLegit.Domain.Advisor;

namespace fasterLegit.Services.Advisor;

public sealed class AdvisorReportStore(AdvisorSettingsStore settings)
{
    public string Save(
        AdvisorReport report,
        IReadOnlyList<AdvisorExecutionResult> executions,
        AdvisorComparison? comparison
    )
    {
        var directory = Path.Combine(settings.DirectoryPath, "Reports");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(
            directory,
            $"{report.CreatedAt:yyyyMMdd-HHmmss}-{report.Id:N}.json"
        );
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(
                new
                {
                    report,
                    executions,
                    comparison,
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        return path;
    }
}
