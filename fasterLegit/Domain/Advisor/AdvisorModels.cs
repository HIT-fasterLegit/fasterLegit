using CommunityToolkit.Mvvm.ComponentModel;

namespace fasterLegit.Domain.Advisor;

public enum AdvisorProvider
{
    Local,
    Api,
}

public enum AdvisorMode
{
    Diagnose,
    Applications,
    Maintenance,
}

public sealed record AdvisorSettings
{
    public AdvisorProvider Provider { get; init; }
    public string LocalEndpoint { get; init; } = "http://127.0.0.1:11434";
    public string LocalModel { get; init; } = "qwen3:4b";
    public string ApiEndpoint { get; init; } = "";
    public string ApiModel { get; init; } = "";
    public int SampleSeconds { get; init; } = 15;
    public string Endpoint => Provider == AdvisorProvider.Local ? LocalEndpoint : ApiEndpoint;
    public string Model => Provider == AdvisorProvider.Local ? LocalModel : ApiModel;
}

public sealed record AdvisorRequest(
    string Question,
    string Goal,
    AdvisorMode Mode,
    bool AllowRemoteData,
    IReadOnlyList<string> Conversation
);

public sealed record AdvisorEvidence(string Id, string Title, string Value, string Source);

public sealed record AdvisorFinding(
    string Title,
    string Explanation,
    IReadOnlyList<string> EvidenceIds
);

public sealed record AdvisorProcess(string Name, double MemoryMb, double? CpuPercent);

public sealed record AdvisorSample(
    double? CpuPercent,
    double MemoryPercent,
    long AvailableMemoryBytes
);

public sealed record AdvisorMeasurement(
    DateTimeOffset StartedAt,
    int DurationSeconds,
    IReadOnlyList<AdvisorSample> Samples,
    IReadOnlyList<AdvisorProcess> Processes
);

public enum AdvisorActionKind
{
    ReviewStartup,
    ReviewApplications,
    ReviewCleanup,
    ReviewOptimizations,
    DisableStartup,
    RemoveApplication,
    Cleanup,
}

public sealed record AdvisorAction(
    string Id,
    string Title,
    string Description,
    AdvisorActionKind Kind,
    string TargetId = ""
);

public partial class AdvisorActionChoice(AdvisorAction action) : ObservableObject
{
    public AdvisorAction Action { get; } = action;
    public string Title => Action.Title;
    public string Description => Action.Description;

    [ObservableProperty]
    private bool _isSelected;
}

public sealed record AdvisorReport(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Summary,
    string FollowUpQuestion,
    IReadOnlyList<AdvisorFinding> Findings,
    IReadOnlyList<AdvisorEvidence> Evidence,
    IReadOnlyList<AdvisorAction> Actions,
    AdvisorMeasurement? Measurement,
    IReadOnlyList<string> ToolsUsed,
    string Provider,
    string Model
);

public sealed record AdvisorExecutionResult(
    string ActionId,
    string Title,
    string Outcome,
    bool Succeeded
);

public sealed record AdvisorComparison(
    double? CpuBefore,
    double? CpuAfter,
    double MemoryBefore,
    double MemoryAfter,
    int SecondsBefore,
    int SecondsAfter
);

public interface IAdvisorModelClient
{
    Task<string> CompleteAsync(
        string instruction,
        string data,
        CancellationToken cancellationToken,
        string? responseSchema = null
    );
}

public interface IAdvisorEvidenceCollector
{
    Task<AdvisorCollection> CollectAsync(
        IReadOnlyList<string> tools,
        string question,
        int sampleSeconds,
        IProgress<string> progress,
        CancellationToken cancellationToken
    );
    Task<AdvisorMeasurement> MeasureAsync(int seconds, CancellationToken cancellationToken);
}

public sealed class AdvisorCollection
{
    public List<AdvisorEvidence> Evidence { get; } = [];
    public List<AdvisorAction> Actions { get; } = [];
    public AdvisorMeasurement? Measurement { get; set; }
}
