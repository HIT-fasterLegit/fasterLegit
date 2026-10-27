using System.Diagnostics;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fasterLegit.Common.Helpers;
using fasterLegit.Domain.Configuration;
using fasterLegit.Resources.Languages;
using fasterLegit.Services.Configuration;
using fasterLegit.UI.Behaviors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace fasterLegit.UI.ViewModels.Pages;

public partial class SettingsViewModel(
    ConfigManager configManager,
    IOptionsMonitor<AppSettings> appOptionsMonitor,
    ISnackbarService snackbarService,
    ILogger<SettingsViewModel> logger
) : ViewModel
{
    [ObservableProperty]
    private ApplicationTheme _currentApplicationTheme = ApplicationTheme.Unknown;

    [ObservableProperty]
    private bool _smoothScrolling;

    protected override Task InitializeOnceAsync()
    {
        SmoothScrolling = appOptionsMonitor.CurrentValue.Optimize.SmoothScrolling;
        SmoothScrollBehavior.GlobalEnabled = SmoothScrolling;
        CurrentApplicationTheme = ApplicationThemeManager.GetAppTheme();

        ApplicationThemeManager.Changed += OnThemeChanged;

        return Task.CompletedTask;
    }

    private void OnThemeChanged(ApplicationTheme currentApplicationTheme, Color systemAccent)
    {
        if (CurrentApplicationTheme != currentApplicationTheme)
            CurrentApplicationTheme = currentApplicationTheme;
    }

    private async Task SaveConfigAsync(Func<Task> saveAction, Func<Task>? revertAction = null)
    {
        try
        {
            await saveAction();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save configuration");
            if (revertAction != null)
            {
                try
                {
                    await revertAction();
                }
                catch (Exception revertEx)
                {
                    logger.LogError(revertEx, "Failed to revert UI property");
                }
            }
        }
    }

    [RelayCommand]
    private void OpenRootDir()
    {
        try
        {
            logger.LogInformation("Opening root directory: {Path}", Shared.RootDirectory);
            Process.Start(
                new ProcessStartInfo { FileName = Shared.RootDirectory, UseShellExecute = true }
            );
        }
        catch (Exception ex)
        {
            snackbarService.Show(
                Loc.Instance["Snackbar.OpenFailed.Title"],
                Loc.Instance["Snackbar.OpenFailed.Message"],
                ControlAppearance.Danger,
                new SymbolIcon { Symbol = SymbolRegular.ErrorCircle24, Filled = true },
                TimeSpan.FromSeconds(5)
            );
            logger.LogError(ex, "Failed to open root directory: {Path}", Shared.RootDirectory);
        }
    }

    [RelayCommand]
    private async Task ToggleSmoothScrolling()
    {
        if (!IsInitialized)
            return;
        try
        {
            var newValue = !appOptionsMonitor.CurrentValue.Optimize.SmoothScrolling;
            await configManager.SetAsync(x => x.Optimize.SmoothScrolling, newValue);
            SmoothScrolling = newValue;
            SmoothScrollBehavior.GlobalEnabled = SmoothScrolling;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to toggle SmoothScrolling setting");
            SmoothScrolling = appOptionsMonitor.CurrentValue.Optimize.SmoothScrolling;
        }
    }

    partial void OnCurrentApplicationThemeChanged(
        ApplicationTheme oldValue,
        ApplicationTheme newValue
    )
    {
        if (!IsInitialized)
            return;

        ApplicationThemeManager.Apply(newValue, updateAccent: false);
        App.ApplyAccentFor(newValue);

        _ = SaveConfigAsync(
            async () =>
            {
                await configManager.SetAsync(x => x.App.Theme, newValue);
            },
            () =>
            {
                CurrentApplicationTheme = oldValue;
                return Task.CompletedTask;
            }
        );
    }
}
