using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using fasterLegit.Common.Helpers;
using fasterLegit.Domain.Advisor;
using fasterLegit.Domain.Optimizations.Models.Bloatware;
using fasterLegit.Domain.Optimizations.Models.StartupManager;
using fasterLegit.Services.Configuration;
using fasterLegit.Services.System;
using fasterLegit.Services.UI;

namespace fasterLegit.Services.Advisor;

public sealed class AdvisorEvidenceCollector(
    SystemInfoService systemInfo,
    StartupManagerService startup,
    BloatwareService bloatware
) : IAdvisorEvidenceCollector
{
    public async Task<AdvisorCollection> CollectAsync(
        IReadOnlyList<string> tools,
        string question,
        int sampleSeconds,
        IProgress<string> progress,
        CancellationToken cancellationToken
    )
    {
        var result = new AdvisorCollection();
        var snapshot = await systemInfo
            .EnsureSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);
        result.Evidence.Add(
            new(
                "system",
                Loc.Instance["AI.Evidence.System"],
                $"Windows build {snapshot.Windows.BuildNumber}; CPU {snapshot.Cpu.Name}; RAM {snapshot.Memory.TotalGB:0.0} GB",
                "Windows / SystemInfoService"
            )
        );
        foreach (var tool in tools.Distinct(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(Loc.Instance["AI.Tool." + tool]);
            switch (tool)
            {
                case "measure_resources":
                    result.Measurement = await MeasureAsync(sampleSeconds, cancellationToken)
                        .ConfigureAwait(false);
                    var samples = result.Measurement.Samples;
                    var cpu = samples
                        .Where(s => s.CpuPercent.HasValue)
                        .Select(s => s.CpuPercent!.Value)
                        .ToArray();
                    result.Evidence.Add(
                        new(
                            "resources",
                            Loc.Instance["AI.Evidence.Resources"],
                            JsonSerializer.Serialize(
                                new
                                {
                                    measured_seconds = result.Measurement.DurationSeconds,
                                    samples = samples.Count,
                                    cpu_average_percent = cpu.Length == 0
                                        ? (double?)null
                                        : Math.Round(cpu.Average(), 1),
                                    memory_average_percent = Math.Round(
                                        samples.Average(s => s.MemoryPercent),
                                        1
                                    ),
                                    memory_peak_percent = Math.Round(
                                        samples.Max(s => s.MemoryPercent),
                                        1
                                    ),
                                    high_memory_sample_count = samples.Count(s =>
                                        s.MemoryPercent >= 90
                                    ),
                                    top_processes = result.Measurement.Processes,
                                    limitations = "No boot trace, thermal, GPU, paging or disk activity measurements. Sampling is observation, not proof of cause.",
                                }
                            ),
                            "Windows native counters / process working sets"
                        )
                    );
                    break;
                case "inspect_storage":
                    var volumes = systemInfo.GetLiveStorage().Volumes;
                    result.Evidence.Add(
                        new(
                            "storage",
                            Loc.Instance["AI.Evidence.Storage"],
                            JsonSerializer.Serialize(
                                volumes.Select(v => new
                                {
                                    drive = v.DriveLetter,
                                    free_gb = Math.Round(v.FreeBytes / 1073741824.0, 1),
                                    total_gb = Math.Round(v.TotalBytes / 1073741824.0, 1),
                                    media = v.MediaType.ToString(),
                                })
                            ),
                            "Windows / SystemInfoService"
                        )
                    );
                    foreach (var id in new[] { "TempFiles", "SystemTemp" })
                        result.Actions.Add(
                            new(
                                "cleanup:" + id,
                                Loc.Instance["AI.Action.Cleanup", id],
                                Loc.Instance["AI.Action.Cleanup.Detail"],
                                AdvisorActionKind.Cleanup,
                                id
                            )
                        );
                    break;
                case "inspect_startup":
                    var entries = await startup.GetStartupAppsAsync().ConfigureAwait(false);
                    result.Evidence.Add(
                        new(
                            "startup",
                            Loc.Instance["AI.Evidence.Startup"],
                            JsonSerializer.Serialize(
                                new
                                {
                                    total = entries.Count,
                                    enabled = entries.Count(x => x.IsEnabled),
                                    boot_impact_measured = false,
                                    entries = entries
                                        .Where(x => x.IsEnabled)
                                        .Take(12)
                                        .Select(x => new
                                        {
                                            id = StartupId(x),
                                            name = Limit(x.Name),
                                            publisher = Limit(x.Publisher),
                                            source = x.Location.ToString(),
                                        }),
                                }
                            ),
                            "Windows startup entries; no measured boot impact"
                        )
                    );
                    foreach (var item in entries.Where(CanOfferStartupChange).Take(12))
                        result.Actions.Add(
                            new(
                                "startup:" + StartupId(item),
                                Loc.Instance["AI.Action.Startup", Limit(item.Name)],
                                Loc.Instance["AI.Action.Startup.Detail"],
                                AdvisorActionKind.DisableStartup,
                                StartupId(item)
                            )
                        );
                    break;
                case "inspect_apps":
                    var apps = await bloatware.GetAppXPackagesAsync().ConfigureAwait(false);
                    var manifests = apps.ToDictionary(
                        x => x.PackageFullName,
                        x => ReadDependencies(x.InstallLocation),
                        StringComparer.Ordinal
                    );
                    var selected = apps.OrderByDescending(x => QueryMatches(question, x.Name))
                        .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        .Take(12)
                        .ToArray();
                    foreach (var app in selected)
                    {
                        var dependents = apps.Where(x =>
                                x.PackageFullName != app.PackageFullName
                                && manifests[x.PackageFullName]
                                    ?.Contains(app.Name, StringComparer.OrdinalIgnoreCase) == true
                            )
                            .ToArray();
                        var exactRule =
                            Shared.SafeApps.Contains(app.Name)
                            || Shared.CautionApps.Contains(app.Name);
                        result.Evidence.Add(
                            new(
                                "app:" + AppId(app),
                                Limit(app.Name),
                                JsonSerializer.Serialize(
                                    new
                                    {
                                        name = Limit(app.Name),
                                        version = app.Version,
                                        risk_rule = app.Risk.ToString(),
                                        exact_rule_match = exactRule,
                                        declared_dependencies = manifests[app.PackageFullName],
                                        known_removable_dependents = dependents.Select(x =>
                                            Limit(x.Name)
                                        ),
                                        dependency_coverage = "Only successfully read manifests of removable AppX packages. Other applications and user data are not assessed.",
                                        data_backup_required = true,
                                        reinstall_verified = false,
                                    }
                                ),
                                "Installed AppX metadata / local rule catalog / readable manifests"
                            )
                        );
                        if (
                            app.Risk == AppRisk.Safe
                            && Shared.SafeApps.Contains(app.Name)
                            && dependents.Length == 0
                        )
                            result.Actions.Add(
                                new(
                                    "remove:" + AppId(app),
                                    Loc.Instance["AI.Action.Remove", Limit(app.Name)],
                                    Loc.Instance["AI.Action.Remove.Detail"],
                                    AdvisorActionKind.RemoveApplication,
                                    AppId(app)
                                )
                            );
                    }
                    result.Evidence.Add(
                        new(
                            "apps.scope",
                            Loc.Instance["AI.Evidence.AppScope"],
                            $"{apps.Count} removable AppX packages; {selected.Length} included. Not an inventory of all Win32 applications. Application necessity and data safety are user decisions.",
                            "BloatwareService / bounded inventory"
                        )
                    );
                    break;
                default:
                    throw new InvalidDataException("AI.Error.Tool");
            }
        }
        result.Evidence.AddRange(AdvisorKnowledge.Retrieve(question));
        result.Actions.AddRange(
            new[]
            {
                new AdvisorAction(
                    "review:startup",
                    Loc.Instance["AI.Action.ReviewStartup"],
                    Loc.Instance["AI.Action.Review.Detail"],
                    AdvisorActionKind.ReviewStartup
                ),
                new AdvisorAction(
                    "review:apps",
                    Loc.Instance["AI.Action.ReviewApps"],
                    Loc.Instance["AI.Action.Review.Detail"],
                    AdvisorActionKind.ReviewApplications
                ),
                new AdvisorAction(
                    "review:cleanup",
                    Loc.Instance["AI.Action.ReviewCleanup"],
                    Loc.Instance["AI.Action.Review.Detail"],
                    AdvisorActionKind.ReviewCleanup
                ),
                new AdvisorAction(
                    "review:optimizations",
                    Loc.Instance["AI.Action.ReviewOptimizations"],
                    Loc.Instance["AI.Action.Review.Detail"],
                    AdvisorActionKind.ReviewOptimizations
                ),
            }
        );
        return result;
    }

    public async Task<AdvisorMeasurement> MeasureAsync(
        int seconds,
        CancellationToken cancellationToken
    )
    {
        if (seconds is < 5 or > 60)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        var start = DateTimeOffset.UtcNow;
        var clock = Stopwatch.StartNew();
        var processBefore = ReadProcesses();
        var previous = ReadCpuTimes();
        var samples = new List<AdvisorSample>();
        for (var i = 0; i < seconds; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            var next = ReadCpuTimes();
            double? cpu = null;
            if (previous is { } before && next is { } after && after.Total > before.Total)
                cpu = Math.Clamp(
                    100.0 * (1 - (after.Idle - before.Idle) / (double)(after.Total - before.Total)),
                    0,
                    100
                );
            var memory = systemInfo.GetLiveMemory();
            if (memory.TotalBytes <= 0)
                throw new InvalidDataException("AI.Error.Measurement");
            samples.Add(new(cpu, memory.UsedPercent, memory.AvailableBytes));
            previous = next;
        }
        var elapsed = clock.Elapsed.TotalSeconds;
        var processes = ReadProcesses()
            .Select(x =>
            {
                double? cpu = processBefore.TryGetValue(x.Key, out var before)
                    ? Math.Clamp(
                        (x.Value.Cpu - before.Cpu).TotalSeconds
                            / elapsed
                            / Environment.ProcessorCount
                            * 100,
                        0,
                        100
                    )
                    : null;
                return new AdvisorProcess(
                    Limit(x.Value.Name),
                    Math.Round(x.Value.Memory / 1048576.0, 1),
                    cpu.HasValue ? Math.Round(cpu.Value, 1) : null
                );
            })
            .OrderByDescending(x => x.MemoryMb)
            .Take(12)
            .ToArray();
        return new(start, (int)Math.Round(elapsed), samples, processes);
    }

    public static string StartupId(StartupApp app) =>
        Hash($"{app.Location}|{app.PathOrKey}|{app.OriginalValueNameOrFileName}|{app.Name}");

    public static string AppId(AppXPackage app) => Hash(app.PackageFullName);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16];

    public static bool CanOfferStartupChange(StartupApp app) =>
        app.IsEnabled
        && app.Location
            is StartupAppLocation.RegistryHKCURun
                or StartupAppLocation.UserStartupFolder
        && !new[]
        {
            "security",
            "defender",
            "antivirus",
            "driver",
            "backup",
            "onedrive",
            "realtek",
            "nvidia",
            "intel",
            "amd",
        }.Any(word =>
            (app.Name + " " + app.Publisher).Contains(word, StringComparison.OrdinalIgnoreCase)
        );

    public static IReadOnlyList<string>? ReadDependencies(string installLocation)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(installLocation))
                return null;
            var path = Path.Combine(installLocation, "AppxManifest.xml");
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > 1048576)
                return null;
            using var reader = XmlReader.Create(
                path,
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }
            );
            var doc = XDocument.Load(reader);
            return doc.Descendants()
                .Where(x => x.Parent?.Name.LocalName == "Dependencies")
                .Select(x => x.Attribute("Name")?.Value)
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
        {
            return null;
        }
    }

    private static bool QueryMatches(string query, string name) =>
        query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 4)
            .Any(w => name.Contains(w, StringComparison.OrdinalIgnoreCase));

    private static string Limit(string value) =>
        new(value.Where(c => !char.IsControl(c)).Take(120).ToArray());

    private static Dictionary<int, (string Name, long Memory, TimeSpan Cpu)> ReadProcesses()
    {
        var values = new Dictionary<int, (string, long, TimeSpan)>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
                try
                {
                    values[process.Id] = (
                        process.ProcessName,
                        process.WorkingSet64,
                        process.TotalProcessorTime
                    );
                }
                catch (Exception ex)
                    when (ex
                            is InvalidOperationException
                                or global::System.ComponentModel.Win32Exception
                                or NotSupportedException
                    ) { }
        }
        return values;
    }

    private static (ulong Idle, ulong Total)? ReadCpuTimes()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
            return null;
        static ulong Value(global::System.Runtime.InteropServices.ComTypes.FILETIME time) =>
            ((ulong)(uint)time.dwHighDateTime << 32) | (uint)time.dwLowDateTime;
        return (Value(idle), Value(kernel) + Value(user));
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out global::System.Runtime.InteropServices.ComTypes.FILETIME idle,
        out global::System.Runtime.InteropServices.ComTypes.FILETIME kernel,
        out global::System.Runtime.InteropServices.ComTypes.FILETIME user
    );
}
