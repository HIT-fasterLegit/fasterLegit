using System.IO;
using fasterLegit.Domain.Advisor;
using fasterLegit.Domain.Optimizations.Models.StartupManager;
using fasterLegit.Services.Advisor;

namespace fasterLegit.Test.Services.Advisor;

public class AdvisorSafetyTests
{
    [Theory]
    [InlineData("http://example.com/v1")]
    [InlineData("https://user:password@example.com/v1")]
    [InlineData("https://example.com/v1?key=private")]
    [InlineData("https://example.com/v1#fragment")]
    [InlineData("file:///C:/secret")]
    public void ApiEndpoint_UnsafeAddress_IsRejected(string address)
    {
        Assert.Throws<ArgumentException>(() =>
            AdvisorSettingsStore.Validate(
                new AdvisorSettings
                {
                    Provider = AdvisorProvider.Api,
                    ApiEndpoint = address,
                    ApiModel = "test-model",
                }
            )
        );
    }

    [Theory]
    [InlineData("https://remote.example", "qwen3:4b")]
    [InlineData("http://127.0.0.1:11434", "model:cloud")]
    [InlineData("http://127.0.0.1:11434", "model:large-cloud")]
    public void LocalEndpoint_RemoteOrCloudModel_IsRejected(string address, string model)
    {
        Assert.Throws<ArgumentException>(() =>
            AdvisorSettingsStore.Validate(
                new AdvisorSettings { LocalEndpoint = address, LocalModel = model }
            )
        );
    }

    [Fact]
    public void Reply_UnknownAction_IsRejectedBeforeExecution()
    {
        var data = Collection();
        Assert.Throws<InvalidDataException>(() =>
            AdvisorService.ValidateReply(
                """{"summary":"test","findings":[],"actionIds":["run:powershell"]}""",
                data,
                [],
                new()
            )
        );
    }

    [Fact]
    public void Reply_FabricatedEvidence_IsRejected()
    {
        Assert.Throws<InvalidDataException>(() =>
            AdvisorService.ValidateReply(
                """{"summary":"test","findings":[{"title":"temperature","explanation":"unmeasured","evidenceIds":["thermal"]}],"actionIds":[]}""",
                Collection(),
                [],
                new()
            )
        );
    }

    [Fact]
    public void Reply_ValidEvidenceAndAllowlistedAction_AreRetained()
    {
        var report = AdvisorService.ValidateReply(
            """{"summary":"Observed memory pressure, not a proven cause.","findings":[{"title":"Memory","explanation":"Measured sample","evidenceIds":["resources"]}],"actionIds":["review:startup"]}""",
            Collection(),
            ["measure_resources"],
            new()
        );
        Assert.Single(report.Findings);
        Assert.Equal("review:startup", Assert.Single(report.Actions).Id);
        Assert.Equal("resources", Assert.Single(report.Findings[0].EvidenceIds));
    }

    [Fact]
    public void Selection_ExpiredPlan_IsRejected()
    {
        var report = Report() with { CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-31) };
        Assert.Throws<InvalidOperationException>(() =>
            AdvisorActionExecutor.ValidateSelection(
                report,
                ["review:startup"],
                DateTimeOffset.UtcNow
            )
        );
    }

    [Fact]
    public void Selection_ActionOutsidePlan_IsRejected()
    {
        Assert.Throws<InvalidDataException>(() =>
            AdvisorActionExecutor.ValidateSelection(
                Report(),
                ["review:apps"],
                DateTimeOffset.UtcNow
            )
        );
    }

    [Fact]
    public void Selection_ForgedCleanupTarget_IsRejected()
    {
        var report = Report() with
        {
            Actions =
            [
                new("cleanup:Windows.old", "bad", "bad", AdvisorActionKind.Cleanup, "Windows.old"),
            ],
        };
        Assert.Throws<InvalidDataException>(() =>
            AdvisorActionExecutor.ValidateSelection(
                report,
                ["cleanup:Windows.old"],
                DateTimeOffset.UtcNow
            )
        );
    }

    [Fact]
    public void Comparison_UsesMeasuredAverages_AndPreservesMissingCpu()
    {
        var before = new AdvisorMeasurement(
            DateTimeOffset.UtcNow,
            5,
            [new(null, 80, 1), new(null, 100, 1)],
            []
        );
        var after = new AdvisorMeasurement(
            DateTimeOffset.UtcNow,
            5,
            [new(10, 60, 1), new(30, 80, 1)],
            []
        );
        var result = AdvisorService.Compare(before, after);
        Assert.Null(result.CpuBefore);
        Assert.Equal(20, result.CpuAfter);
        Assert.Equal(90, result.MemoryBefore);
        Assert.Equal(70, result.MemoryAfter);
    }

    [Fact]
    public void Knowledge_VietnameseMemoryQuery_RetrievesRelevantCitedGuide()
    {
        var entries = AdvisorKnowledge.Retrieve("máy thiếu bộ nhớ RAM khi mở trình duyệt");
        Assert.Contains(entries, x => x.Id == "guide.memory");
        Assert.All(entries, x => Assert.StartsWith("https://", x.Source));
    }

    [Theory]
    [InlineData("Windows Security")]
    [InlineData("OneDrive")]
    [InlineData("NVIDIA Driver")]
    public void Startup_ProtectedRole_IsNotOfferedForAiDisable(string name)
    {
        var item = new StartupApp
        {
            Name = name,
            Location = StartupAppLocation.RegistryHKCURun,
            PathOrKey = "test",
            IsEnabled = true,
        };
        Assert.False(AdvisorEvidenceCollector.CanOfferStartupChange(item));
    }

    private static AdvisorCollection Collection()
    {
        var result = new AdvisorCollection();
        result.Evidence.Add(new("resources", "Memory", "90 percent", "test measurement"));
        result.Actions.Add(
            new("review:startup", "Review", "No mutation", AdvisorActionKind.ReviewStartup)
        );
        return result;
    }

    private static AdvisorReport Report() =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "test",
            "",
            [],
            [],
            [new("review:startup", "Review", "No mutation", AdvisorActionKind.ReviewStartup)],
            null,
            [],
            "Local",
            "test"
        );
}
