using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fasterLegit.Common.Helpers;
using fasterLegit.Services.Configuration;
using Microsoft.Extensions.Logging;
using Wpf.Ui.Appearance;

namespace fasterLegit.UI.ViewModels.Dialogs;

public partial class LegalDialogViewModel(
    ConfigManager configManager,
    ILogger<LegalDialogViewModel> logger
) : ViewModel
{
    [ObservableProperty]
    private ApplicationTheme _currentApplicationTheme = ApplicationTheme.Unknown;

    protected override Task InitializeOnceAsync()
    {
        CurrentApplicationTheme = ApplicationThemeManager.GetAppTheme();
        ApplicationThemeManager.Changed += OnThemeChanged;
        return Task.CompletedTask;
    }

    private void OnThemeChanged(
        ApplicationTheme currentApplicationTheme,
        System.Windows.Media.Color systemAccent
    )
    {
        if (CurrentApplicationTheme != currentApplicationTheme)
            CurrentApplicationTheme = currentApplicationTheme;
    }

    partial void OnCurrentApplicationThemeChanged(
        ApplicationTheme oldValue,
        ApplicationTheme newValue
    )
    {
        if (!IsInitialized)
            return;
        // Theme first: ApplyAccentFor reads GetAppTheme() cache to pick
        // AccentFillColorDefault (secondary for Dark, primary for Light).
        ApplicationThemeManager.Apply(newValue, updateAccent: false);
        App.ApplyAccentFor(newValue);
        _ = SafeFireAndForgetAsync(
            async () => await configManager.SetAsync(x => x.App.Theme, newValue),
            async () =>
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    CurrentApplicationTheme = oldValue
                )
        );
    }

    [RelayCommand]
    private void OpenWebsite(string type)
    {
        try
        {
            var url = type switch
            {
                "GitHub" => Shared.GitHubRepoURL,
                "Documentation" or "Docs" => Shared.WebsiteURL + "docs/guides/getting-started",
                "Discord" => Shared.DiscordInviteURL,
                "Acknowledgements" => Shared.AcknowledgementsURL,
                _ => null,
            };
            if (url == null)
                return;
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open link {Type}", type);
        }
    }

    private async Task SafeFireAndForgetAsync(
        Func<Task> taskFactory,
        Func<Task>? revertAction = null
    )
    {
        try
        {
            await taskFactory();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save setting");
            if (revertAction != null)
            {
                try
                {
                    await revertAction();
                }
                catch (Exception revertEx)
                {
                    logger.LogError(revertEx, "Failed to revert setting");
                }
            }
        }
    }
}
