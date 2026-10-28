using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fasterLegit.Common.Helpers;
using fasterLegit.Resources.Languages;
using fasterLegit.Services.Configuration;
using fasterLegit.Services.System;
using Microsoft.Extensions.Logging;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace fasterLegit.UI.ViewModels.Pages;

public partial class DashboardViewModel : ViewModel
{
    private readonly IContentDialogService _contentDialogService;
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly ISnackbarService _snackbarService;
    private readonly SystemInfoService _systemInfoService;

    private readonly DispatcherTimer _updateTimer;
    private bool _ticking;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasSystemInfoLoadFailed;

    [ObservableProperty]
    private StorageInfo _runtimeStorage = StorageInfo.Unknown;

    [ObservableProperty]
    private MemoryInfo _runtimeMemory = MemoryInfo.Unknown;

    [ObservableProperty]
    private SystemInfo _systemInfo = SystemInfo.Unknown;

    [ObservableProperty]
    private string _windowsTitle = Loc.Instance["Common.Unknown"];

    [ObservableProperty]
    private string _windowsEditionDisplay = Loc.Instance["Common.Unknown"];

    [ObservableProperty]
    private string _installDateText = Loc.Instance["Common.Unknown"];

    [ObservableProperty]
    private string _lastBootText = Loc.Instance["Common.Unknown"];

    [ObservableProperty]
    private string _uptimeText = Loc.Instance["Common.Unknown"];

    [ObservableProperty]
    private string _powerPlanText = Loc.Instance["Common.Unknown"];

    public DashboardViewModel(
        SystemInfoService systemInfoService,
        ISnackbarService snackbarService,
        ILogger<DashboardViewModel> logger,
        IContentDialogService contentDialogService
    )
    {
        _systemInfoService = systemInfoService;
        _snackbarService = snackbarService;
        _logger = logger;
        _contentDialogService = contentDialogService;

        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _updateTimer.Tick += OnUpdateTick;
    }

    protected override async Task InitializeOnceAsync()
    {
        await LoadSystemInfoAsync();
        _systemInfoService.LogSummary();
    }

    /// <inheritdoc />
    public override async Task OnNavigatedToAsync()
    {
        await base.OnNavigatedToAsync();

        if (IsLoading)
            return;

        await LoadSystemInfoAsync();
        _updateTimer.Start();
    }

    /// <inheritdoc />
    public override Task OnNavigatedFromAsync()
    {
        _updateTimer.Stop();
        return base.OnNavigatedFromAsync();
    }

    protected override void OnLanguageChanged(CultureInfo newCulture)
    {
        ApplyDisplayTexts(SystemInfo);
    }

    #region Commands

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadSystemInfoAsync();
    }

    [RelayCommand]
    private void SelectDisk(StorageVolume diskVolume)
    {
        if (diskVolume is null || string.IsNullOrWhiteSpace(diskVolume.DriveLetter))
            return;

        try
        {
            var drivePath = $"{diskVolume.DriveLetter}\\";

            Process.Start(new ProcessStartInfo { FileName = drivePath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _snackbarService.Show(
                Loc.Instance["Snackbar.OpenFailed.Title"],
                Loc.Instance["Snackbar.OpenFailed.Message"],
                ControlAppearance.Danger,
                new SymbolIcon { Symbol = SymbolRegular.ErrorCircle24, Filled = true },
                TimeSpan.FromSeconds(5)
            );
            _logger.LogError(
                ex,
                "Failed to open disk volume {DriveLetter}",
                diskVolume.DriveLetter
            );
        }
    }

    [RelayCommand]
    private void Run(string action)
    {
        try
        {
            switch (action)
            {
                case "Settings":
                    Process.Start(
                        new ProcessStartInfo { FileName = "ms-settings:", UseShellExecute = true }
                    );
                    break;

                case "TaskManager":
                    Process.Start(
                        new ProcessStartInfo { FileName = "taskmgr", UseShellExecute = true }
                    );
                    break;

                case "ControlPanel":
                    Process.Start(
                        new ProcessStartInfo { FileName = "control", UseShellExecute = true }
                    );
                    break;
            }
        }
        catch (Exception ex)
        {
            _snackbarService.Show(
                Loc.Instance["Snackbar.OpenFailed.Title"],
                Loc.Instance["Snackbar.OpenFailed.Message"],
                ControlAppearance.Danger,
                new SymbolIcon { Symbol = SymbolRegular.ErrorCircle24, Filled = true },
                TimeSpan.FromSeconds(5)
            );
            _logger.LogError(ex, "Failed to run action {Action}", action);
        }
    }

    #endregion Commands

    #region Helpers

    private async Task LoadSystemInfoAsync()
    {
        IsLoading = true;
        HasSystemInfoLoadFailed = false;

        try
        {
            var snapshot = await _systemInfoService.RefreshAsync();
            SystemInfo = snapshot;
            RuntimeMemory = snapshot.Memory;
            RuntimeStorage = snapshot.Storage;
            ApplyDisplayTexts(snapshot);
        }
        catch (Exception ex)
        {
            HasSystemInfoLoadFailed = true;
            _logger.LogError(ex, "Failed to load system information");
            _snackbarService.Show(
                Loc.Instance["Snackbar.OpenFailed.Title"],
                Loc.Instance["Snackbar.OpenFailed.Message"],
                ControlAppearance.Danger,
                new SymbolIcon { Symbol = SymbolRegular.ErrorCircle24, Filled = true },
                TimeSpan.FromSeconds(5)
            );
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    ///     Runs on the two second tick, on the UI thread and through the cheap cached service
    ///     paths, so a slow scan can never overlap ticks.
    /// </summary>
    private void OnUpdateTick(object? sender, EventArgs e)
    {
        if (_ticking)
            return;
        _ticking = true;
        try
        {
            RuntimeMemory = _systemInfoService.GetLiveMemory();
            RuntimeStorage = _systemInfoService.GetLiveStorage();
            UptimeText =
                Loc.Instance["Dashboard.SystemInfo.Uptime.Label"]
                + ": "
                + FormatUptime(TimeSpan.FromMilliseconds((double)Environment.TickCount64));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update runtime info");
        }
        finally
        {
            _ticking = false;
        }
    }

    private void ApplyDisplayTexts(SystemInfo snapshot)
    {
        var unknown = Loc.Instance["Common.Unknown"];
        WindowsTitle = snapshot.Windows switch
        {
            { IsWindows11: true } => "Windows 11",
            { IsWindows10: true } => "Windows 10",
            { BuildNumber: { } build } => $"Windows (build {build})",
            _ => unknown,
        };
        InstallDateText = snapshot.Windows.InstallDate?.ToString("yyyy-MM-dd") ?? unknown;
        LastBootText = snapshot.Windows.LastBootTime?.ToString("yyyy-MM-dd HH:mm") ?? unknown;
        WindowsEditionDisplay = FormatEditionDisplay(
            snapshot.Windows.EditionId,
            snapshot.Windows.Edition
        );
        PowerPlanText = FormatPowerPlan(snapshot.Power);
        UptimeText =
            Loc.Instance["Dashboard.SystemInfo.Uptime.Label"]
            + ": "
            + FormatUptime(TimeSpan.FromMilliseconds((double)Environment.TickCount64));
    }

    internal static string FormatEditionDisplay(string? rawEditionId, WindowsEdition edition)
    {
        var raw = string.IsNullOrWhiteSpace(rawEditionId) ? null : rawEditionId.Trim();
        if (raw is null)
            return edition == WindowsEdition.Unknown
                ? Loc.Instance["Common.Unknown"]
                : ResolveEditionFriendly(edition);
        if (edition == WindowsEdition.Unknown)
            return raw;
        var friendly = ResolveEditionFriendly(edition);
        return raw.Equals(friendly, StringComparison.OrdinalIgnoreCase)
            ? raw
            : $"{raw} ({friendly})";
    }

    private static string ResolveEditionFriendly(WindowsEdition edition)
    {
        var key = $"Enum.WindowsEdition.{edition}";
        var text = Loc.Instance[key];
        return text == key ? edition.ToString() : text;
    }

    private static string FormatPowerPlan(PowerInfo power)
    {
        // Live name from Windows; GUID fallback when the name is unreadable.
        var label = power.SchemeName ?? power.SchemeId?.ToString();
        if (string.IsNullOrWhiteSpace(label))
            return Loc.Instance["Common.Unknown"];
        return Loc.Instance["Dashboard.SystemInfo.PowerPlan", label];
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime < TimeSpan.Zero)
            return Loc.Instance["Common.Unknown"];
        return uptime.TotalDays >= 1 ? $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m"
            : uptime.TotalHours >= 1 ? $"{(int)uptime.TotalHours}h {uptime.Minutes}m"
            : $"{uptime.Minutes}m {uptime.Seconds}s";
    }

    #endregion Helpers
}
