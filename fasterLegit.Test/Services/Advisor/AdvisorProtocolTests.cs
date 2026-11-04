using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using fasterLegit.Domain.Advisor;
using fasterLegit.Services.Advisor;

namespace fasterLegit.Test.Services.Advisor;

public class AdvisorProtocolTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "VeloraAdvisorTests-" + Guid.NewGuid().ToString("N")
    );

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task ApiRequest_UsesBearerAndCompatibleSchema_WhileSettingsExcludePlaintextKey()
    {
        var store = new AdvisorSettingsStore(_directory);
        const string fakeKey = "test-key-not-a-real-credential";
        store.Save(
            new AdvisorSettings
            {
                Provider = AdvisorProvider.Api,
                ApiEndpoint = "https://api.example/v1",
                ApiModel = "test-model",
            },
            fakeKey
        );
        Assert.DoesNotContain(fakeKey, File.ReadAllText(Path.Combine(_directory, "settings.json")));
        Assert.Equal(fakeKey, store.ReadKey());
        var handler = new StubHandler("""{"choices":[{"message":{"content":"{\"ok\":true}"}}]}""");
        using var http = new HttpClient(handler);
        var reply = await new AdvisorModelClient(http, store).CompleteAsync(
            "system",
            "sanitized data",
            CancellationToken.None
        );
        Assert.Equal("{\"ok\":true}", reply);
        Assert.Equal("https://api.example/v1/chat/completions", handler.Uri);
        Assert.Equal("Bearer " + fakeKey, handler.Authorization);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("test-model", body.RootElement.GetProperty("model").GetString());
        Assert.False(body.RootElement.GetProperty("stream").GetBoolean());
        store.ClearKey();
        Assert.False(store.HasKey);
    }

    [Fact]
    public async Task LocalRequest_UsesNativeOllama_AndUnloadsBeforeLaterMeasurements()
    {
        var store = new AdvisorSettingsStore(_directory);
        store.Save(new AdvisorSettings());
        var handler = new StubHandler("""{"message":{"content":"{\"ok\":true}"}}""");
        using var http = new HttpClient(handler);
        await new AdvisorModelClient(http, store).CompleteAsync(
            "system",
            "data",
            CancellationToken.None,
            AdvisorSchemas.Planner
        );
        Assert.Equal("http://127.0.0.1:11434/api/chat", handler.Uri);
        Assert.Null(handler.Authorization);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal(0, body.RootElement.GetProperty("keep_alive").GetInt32());
        Assert.False(body.RootElement.GetProperty("think").GetBoolean());
        Assert.Equal(
            "object",
            body.RootElement.GetProperty("format").GetProperty("type").GetString()
        );
        Assert.Contains("inspect_apps", body.RootElement.GetProperty("format").GetRawText());
    }

    [Theory]
    [InlineData(AdvisorMode.Diagnose, "measure_resources")]
    [InlineData(AdvisorMode.Applications, "inspect_apps")]
    [InlineData(AdvisorMode.Maintenance, "inspect_startup")]
    public async Task ConcreteRequest_IgnoresUnnecessaryFollowUp_AndRunsRequiredChecks(
        AdvisorMode mode,
        string requiredTool
    )
    {
        var store = new AdvisorSettingsStore(_directory);
        store.Save(new());
        var model = new SequenceModel();
        var collector = new StubCollector();
        var report = await new AdvisorService(model, collector, store).AnalyzeAsync(
            new(
                "Please perform the concrete checks for this maintenance request.",
                "",
                mode,
                false,
                []
            ),
            new Progress<string>(),
            CancellationToken.None
        );
        Assert.Contains(requiredTool, report.ToolsUsed);
        if (mode == AdvisorMode.Maintenance)
            Assert.Equal(4, report.ToolsUsed.Count);
        Assert.Empty(report.FollowUpQuestion);
        Assert.Equal(1, collector.Calls);
    }

    private sealed class SequenceModel : IAdvisorModelClient
    {
        private int _calls;

        public Task<string> CompleteAsync(
            string instruction,
            string data,
            CancellationToken cancellationToken,
            string? responseSchema = null
        )
        {
            Assert.NotNull(responseSchema);
            return Task.FromResult(
                ++_calls == 1
                    ? """{"requestedTools":[],"followUpQuestion":"Choose a sampling duration?"}"""
                    : """{"summary":"No actionable result from this test collection.","findings":[],"actionIds":[]}"""
            );
        }
    }

    [Fact]
    public async Task ApiAnalysis_WithoutConsent_DoesNotCallModelOrCollectors()
    {
        var store = new AdvisorSettingsStore(_directory);
        store.Save(
            new AdvisorSettings
            {
                Provider = AdvisorProvider.Api,
                ApiEndpoint = "https://example.test/v1",
                ApiModel = "test",
            }
        );
        var model = new StubModel("{}");
        var collector = new StubCollector();
        var service = new AdvisorService(model, collector, store);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AnalyzeAsync(
                new("question", "", AdvisorMode.Diagnose, false, []),
                new Progress<string>(),
                CancellationToken.None
            )
        );
        Assert.Equal(0, model.Calls);
        Assert.Equal(0, collector.Calls);
    }

    [Fact]
    public async Task Planner_UnknownTool_DoesNotReachCollector()
    {
        var store = new AdvisorSettingsStore(_directory);
        store.Save(new());
        var model = new StubModel("""{"requestedTools":["run_shell"],"followUpQuestion":""}""");
        var collector = new StubCollector();
        var service = new AdvisorService(model, collector, store);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            service.AnalyzeAsync(
                new("question", "", AdvisorMode.Diagnose, false, []),
                new Progress<string>(),
                CancellationToken.None
            )
        );
        Assert.Equal(0, collector.Calls);
    }

    [Fact]
    public async Task Planner_FollowUp_ReturnsQuestionWithoutScanning()
    {
        var store = new AdvisorSettingsStore(_directory);
        store.Save(new());
        var model = new StubModel(
            """{"requestedTools":[],"followUpQuestion":"When does it occur?"}"""
        );
        var collector = new StubCollector();
        var report = await new AdvisorService(model, collector, store).AnalyzeAsync(
            new("slow", "", AdvisorMode.Diagnose, false, []),
            new Progress<string>(),
            CancellationToken.None
        );
        Assert.Equal("When does it occur?", report.FollowUpQuestion);
        Assert.Empty(report.Actions);
        Assert.Equal(0, collector.Calls);
    }

    [Fact]
    public void Manifest_ExternalEntities_AreRejected()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            Path.Combine(_directory, "AppxManifest.xml"),
            """<!DOCTYPE Package [<!ENTITY xxe SYSTEM "file:///C:/Windows/win.ini">]><Package><Dependencies><PackageDependency Name="&xxe;" /></Dependencies></Package>"""
        );
        Assert.Null(AdvisorEvidenceCollector.ReadDependencies(_directory));
    }

    private sealed class StubHandler(string response) : HttpMessageHandler
    {
        public string? Uri { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Uri = request.RequestUri!.ToString();
            Authorization = request.Headers.Authorization?.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class StubModel(string reply) : IAdvisorModelClient
    {
        public int Calls { get; private set; }

        public Task<string> CompleteAsync(
            string instruction,
            string data,
            CancellationToken cancellationToken,
            string? responseSchema = null
        )
        {
            Calls++;
            return Task.FromResult(reply);
        }
    }

    private sealed class StubCollector : IAdvisorEvidenceCollector
    {
        public int Calls { get; private set; }

        public Task<AdvisorCollection> CollectAsync(
            IReadOnlyList<string> tools,
            string question,
            int sampleSeconds,
            IProgress<string> progress,
            CancellationToken cancellationToken
        )
        {
            Calls++;
            return Task.FromResult(new AdvisorCollection());
        }

        public Task<AdvisorMeasurement> MeasureAsync(
            int seconds,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }
}
