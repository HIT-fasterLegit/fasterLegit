using fasterLegit.Common.Helpers;
using fasterLegit.Domain.Abstractions;
using fasterLegit.Domain.Configuration;
using fasterLegit.Services.Configuration;
using fasterLegit.Services.Optimization;
using fasterLegit.Services.Revert;
using fasterLegit.Services.System;
using fasterLegit.Services.System.Primitives;
using fasterLegit.Services.UI;
using fasterLegit.UI.Pages;
using fasterLegit.UI.ViewModels;
using fasterLegit.UI.ViewModels.Dialogs;
using fasterLegit.UI.ViewModels.Optimizer;
using fasterLegit.UI.ViewModels.Pages;
using fasterLegit.UI.ViewModels.Windows;
using fasterLegit.UI.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace fasterLegit;

/// <summary>
///     Application DI registrations, separated from the hosting bootstrap in
///     <see cref="App"/> so the whole graph can be built in a test.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the WPF shell, pages, view models, and application services.
    /// </summary>
    /// <remarks>
    ///     Every service here is a singleton: the app has a single window and a single
    ///     settings file. Per-operation state (the <c>ChangeSet</c>) is created by
    ///     <c>OptimizationService</c> for each apply, not by DI.
    /// </remarks>
    public static IServiceCollection AddOptimizerApplication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AppSettings>(configuration);

        // Registered explicitly so the graph is buildable without the host builder, which
        // otherwise supplies IConfiguration implicitly.
        services.AddSingleton<IConfiguration>(configuration);

        services.AddNavigationViewPageProvider();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IContentDialogService, ContentDialogService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<DashboardPage>();
        services.AddSingleton<OptimizationCategoryViewModel>();
        services.AddSingleton<OptimizePage>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<SettingsPage>();
        services.AddSingleton<BloatwareViewModel>();
        services.AddSingleton<BloatwarePage>();
        services.AddSingleton<DiskCleanupViewModel>();
        services.AddSingleton<DiskCleanupPage>();
        services.AddSingleton<StartupManagerViewModel>();
        services.AddSingleton<StartupManagerPage>();

        services.AddTransient<LegalDialogViewModel>();
        services.AddTransient<fasterLegit.UI.Dialogs.LegalDialog>();

        services.AddSingleton<ConfigManager>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<ProcessRunner>();
        services.AddSingleton<PowerPlanService>();
        services.AddSingleton<SystemRestoreService>();
        services.AddSingleton<ShellService>();
        services.AddSingleton<RevertManager>();
        services.AddSingleton<OptimizationRegistry>();
        services.AddSingleton<OperationRunner>();
        services.AddSingleton<ToolRunPresenter>();
        services.AddSingleton<OptimizationService>();
        services.AddSingleton<BloatwareService>();
        services.AddSingleton<DiskCleanupService>();
        services.AddSingleton<StartupManagerService>();
        services.AddSingleton<SystemInfoService>();
        services.AddSingleton<StreamService>();
        services.AddSingleton<UpdaterService>();
        services.AddSingleton<UserErrorSurface>();

        return services;
    }
}
