using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using fasterLegit.Common.Helpers;
using fasterLegit.Domain.Advisor;
using fasterLegit.Domain.Execution;
using fasterLegit.Domain.UI;
using fasterLegit.Services.Configuration;
using fasterLegit.Services.Optimization;
using fasterLegit.Services.UI;
using fasterLegit.UI.Pages;
using Microsoft.Extensions.Logging;
using Wpf.Ui;

namespace fasterLegit.Services.Advisor;

public sealed class AdvisorActionExecutor(
    StartupManagerService startup,
    BloatwareService bloatware,
    DiskCleanupService cleanup,
    OperationRunner runner,
    INavigationService navigation,
    ILogger<AdvisorActionExecutor> logger
)
{
    public async Task<IReadOnlyList<AdvisorExecutionResult>> ExecuteAsync(
        AdvisorReport report,
        IReadOnlyList<string> selectedIds,
        CancellationToken cancellationToken,
        IProgress<AdvisorExecutionResult>? progress = null
    )
    {
        var actions = ValidateSelection(report, selectedIds, DateTimeOffset.UtcNow);
        var results = new List<AdvisorExecutionResult>();
        foreach (var action in actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (
                action.Kind
                is AdvisorActionKind.ReviewStartup
                    or AdvisorActionKind.ReviewApplications
                    or AdvisorActionKind.ReviewCleanup
                    or AdvisorActionKind.ReviewOptimizations
            )
            {
                var page = action.Kind switch
                {
                    AdvisorActionKind.ReviewStartup => typeof(StartupManagerPage),
                    AdvisorActionKind.ReviewApplications => typeof(BloatwarePage),
                    AdvisorActionKind.ReviewCleanup => typeof(DiskCleanupPage),
                    _ => typeof(OptimizePage),
                };
                await Application.Current.Dispatcher.InvokeAsync(() => navigation.Navigate(page));
                results.Add(
                    new(action.Id, action.Title, Loc.Instance["AI.Execution.Opened"], true)
                );
                progress?.Report(results[^1]);
                continue;
            }
            var subject = new OperationSubject(
                new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(action.Id)).AsSpan(0, 16)),
                "Advisor:" + action.Id,
                "AI-reviewed maintenance"
            );
            var result = await runner
                .RunAsync(
                    new OperationRequest(
                        subject,
                        action.Title,
                        logger,
                        RevertPersistence.Disabled,
                        async (_, context) =>
                        {
                            context.CancellationToken.ThrowIfCancellationRequested();
                            switch (action.Kind)
                            {
                                case AdvisorActionKind.DisableStartup:
                                    var entries = await startup
                                        .GetStartupAppsAsync()
                                        .ConfigureAwait(false);
                                    var entry = entries.SingleOrDefault(x =>
                                        AdvisorEvidenceCollector.StartupId(x) == action.TargetId
                                    );
                                    if (
                                        entry == null
                                        || !AdvisorEvidenceCollector.CanOfferStartupChange(entry)
                                    )
                                        throw new InvalidOperationException(
                                            Loc.Instance["AI.Error.StaleAction"]
                                        );
                                    var changed = await startup
                                        .ToggleStartupApp(entry, false)
                                        .ConfigureAwait(false);
                                    if (!changed.Ok)
                                        context.Changes.Add(
                                            action.Title,
                                            action.Description,
                                            false,
                                            error: changed.Error
                                        );
                                    else
                                    {
                                        var verified = (
                                            await startup
                                                .GetStartupAppsAsync()
                                                .ConfigureAwait(false)
                                        ).SingleOrDefault(x =>
                                            AdvisorEvidenceCollector.StartupId(x) == action.TargetId
                                        );
                                        if (verified is { IsEnabled: false })
                                            context.Changes.AddIrreversible(
                                                action.Title,
                                                Loc.Instance["AI.Action.Startup.Detail"]
                                            );
                                        else
                                            context.Changes.Add(
                                                action.Title,
                                                action.Description,
                                                false,
                                                error: Loc.Instance["AI.Error.Verify"]
                                            );
                                    }
                                    break;
                                case AdvisorActionKind.RemoveApplication:
                                    var apps = await bloatware
                                        .GetAppXPackagesAsync()
                                        .ConfigureAwait(false);
                                    var app = apps.SingleOrDefault(x =>
                                        AdvisorEvidenceCollector.AppId(x) == action.TargetId
                                    );
                                    if (
                                        app == null
                                        || !Shared.SafeApps.Contains(app.Name)
                                        || apps.Any(x =>
                                            x.PackageFullName != app.PackageFullName
                                            && AdvisorEvidenceCollector
                                                .ReadDependencies(x.InstallLocation)
                                                ?.Contains(
                                                    app.Name,
                                                    StringComparer.OrdinalIgnoreCase
                                                ) == true
                                        )
                                    )
                                        throw new InvalidOperationException(
                                            Loc.Instance["AI.Error.StaleAction"]
                                        );
                                    var removal = await bloatware
                                        .RemoveAppXPackage(app, removeProvisioned: false)
                                        .ConfigureAwait(false);
                                    var stillInstalled = (
                                        await bloatware.GetAppXPackagesAsync().ConfigureAwait(false)
                                    ).Any(x => x.PackageFullName == app.PackageFullName);
                                    if (removal.Succeeded && !stillInstalled)
                                        context.Changes.AddIrreversible(
                                            action.Title,
                                            Loc.Instance["AI.Action.Remove.Detail"]
                                        );
                                    else
                                        context.Changes.Add(
                                            action.Title,
                                            action.Description,
                                            false,
                                            error: Loc.Instance["AI.Error.Verify"]
                                        );
                                    break;
                                case AdvisorActionKind.Cleanup:
                                    if (action.TargetId is not ("TempFiles" or "SystemTemp"))
                                        throw new InvalidDataException("AI.Error.Action");
                                    var item = DiskCleanupService
                                        .GetCleanupItems()
                                        .Single(x => x.Id == action.TargetId);
                                    await cleanup.ScanAsync(item).ConfigureAwait(false);
                                    if (item.SizeBytes == 0)
                                        context.Changes.AddSkip(
                                            action.Title,
                                            Loc.Instance["AI.Execution.Nothing"]
                                        );
                                    else
                                    {
                                        var freed = await cleanup
                                            .CleanAsync(item)
                                            .ConfigureAwait(false);
                                        if (freed > 0)
                                            context.Changes.AddIrreversible(
                                                action.Title,
                                                Loc.Instance["AI.Execution.Freed", freed]
                                            );
                                        else
                                            context.Changes.AddRefused(
                                                action.Title,
                                                Loc.Instance["AI.Execution.Nothing"]
                                            );
                                    }
                                    break;
                                default:
                                    throw new InvalidDataException("AI.Error.Action");
                            }
                            return context.Changes.ToApplyResult();
                        }
                    ),
                    new Progress<ProcessingProgress>(),
                    cancellationToken
                )
                .ConfigureAwait(false);
            results.Add(
                new(
                    action.Id,
                    action.Title,
                    result.Message,
                    result.Status
                        is OptimizationSuccessResult.Success
                            or OptimizationSuccessResult.NothingToDo
                )
            );
            progress?.Report(results[^1]);
        }
        return results;
    }

    public static IReadOnlyList<AdvisorAction> ValidateSelection(
        AdvisorReport report,
        IReadOnlyList<string> ids,
        DateTimeOffset now
    )
    {
        if (
            now - report.CreatedAt > TimeSpan.FromMinutes(30)
            || report.CreatedAt > now.AddMinutes(1)
        )
            throw new InvalidOperationException("AI.Error.StaleAction");
        if (ids.Count is 0 or > 8 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
            throw new InvalidDataException("AI.Error.Action");
        var actions = report.Actions.ToDictionary(x => x.Id, StringComparer.Ordinal);
        if (ids.Any(id => !actions.ContainsKey(id)))
            throw new InvalidDataException("AI.Error.Action");
        var selected = ids.Select(id => actions[id]).ToArray();
        foreach (var action in selected)
        {
            var valid = action.Kind switch
            {
                AdvisorActionKind.DisableStartup => action.Id == "startup:" + action.TargetId
                    && IsOpaqueId(action.TargetId),
                AdvisorActionKind.RemoveApplication => action.Id == "remove:" + action.TargetId
                    && IsOpaqueId(action.TargetId),
                AdvisorActionKind.Cleanup => action.Id == "cleanup:" + action.TargetId
                    && action.TargetId is "TempFiles" or "SystemTemp",
                AdvisorActionKind.ReviewStartup => action.Id == "review:startup",
                AdvisorActionKind.ReviewApplications => action.Id == "review:apps",
                AdvisorActionKind.ReviewCleanup => action.Id == "review:cleanup",
                AdvisorActionKind.ReviewOptimizations => action.Id == "review:optimizations",
                _ => false,
            };
            if (!valid)
                throw new InvalidDataException("AI.Error.Action");
        }
        return selected;
    }

    private static bool IsOpaqueId(string id) => id.Length == 16 && id.All(char.IsAsciiHexDigit);
}
