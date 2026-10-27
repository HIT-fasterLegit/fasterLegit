using System.Reflection;
using fasterLegit.Domain.Abstractions;
using fasterLegit.Domain.Attributes;
using fasterLegit.Domain.Optimizations.Categories;
using fasterLegit.Services.Customize;
using fasterLegit.Services.System;
using fasterLegit.UI.Pages;
using fasterLegit.UI.ViewModels.Dialogs;
using fasterLegit.UI.ViewModels.Optimizer;
using fasterLegit.UI.ViewModels.Pages;
using fasterLegit.UI.ViewModels.Windows;
using fasterLegit.UI.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace fasterLegit.Test.UI;

public class RemovedUiFeaturesTests
{
    [Fact]
    public void Dashboard_LinkCommand_IsNotAvailable()
    {
        Assert.Null(typeof(DashboardViewModel).GetProperty("OpenActionCommand"));
    }

    [Fact]
    public void Optimizer_SourceCommands_AreNotAvailable()
    {
        Assert.Null(typeof(OptimizationCategoryViewModel).GetProperty("ViewSourceOnGitHubCommand"));
        Assert.Null(typeof(OptimizationDetailsViewModel).GetProperty("ViewSourceOnGitHubCommand"));
    }

    [Fact]
    public void ScheduledTasks_PageAndViewModel_AreNotAvailable()
    {
        var assembly = typeof(DashboardViewModel).Assembly;

        Assert.Null(assembly.GetType("fasterLegit.UI.Pages.ScheduledTasksPage"));
        Assert.Null(assembly.GetType("fasterLegit.UI.ViewModels.Pages.ScheduledTasksViewModel"));
    }

    [Fact]
    public void Optimizer_IneffectiveOrRiskyItems_AreNotAvailable()
    {
        var assembly = typeof(UserExperience).Assembly;

        Assert.Null(assembly.GetType("fasterLegit.Domain.Optimizations.Categories.Performance"));
        Assert.Null(
            assembly.GetType("fasterLegit.Domain.Optimizations.Categories.BloatwareAndServices")
        );
        Assert.Null(typeof(UserExperience).GetNestedType("MaximizeUpdatePauseLimit"));
    }

    [Fact]
    public void Customize_RuntimeServices_AreNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddOptimizerApplication(new ConfigurationBuilder().Build());

        Assert.DoesNotContain(
            services,
            service => service.ServiceType == typeof(CustomizeRegistry)
        );
        Assert.DoesNotContain(services, service => service.ServiceType == typeof(CustomizePage));
        Assert.DoesNotContain(
            services,
            service => service.ServiceType == typeof(CustomizeViewModel)
        );
        Assert.DoesNotContain(services, service => service.ServiceType == typeof(IRegistryWatcher));
        Assert.DoesNotContain(
            typeof(MainWindow).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType == typeof(CustomizeRegistry)
        );
    }

    [Fact]
    public void MainWindow_CommunityLinkCommands_AreNotAvailable()
    {
        Assert.Null(typeof(MainWindowViewModel).GetProperty("OpenSupportLinkCommand"));
        Assert.Null(typeof(MainWindowViewModel).GetProperty("OpenDiscordLinkCommand"));
    }

    [Fact]
    public void Settings_RemovedCommands_AreNotAvailable()
    {
        Assert.Null(typeof(SettingsViewModel).GetProperty("ClearDownloadsCommand"));
        Assert.Null(typeof(SettingsViewModel).GetProperty("ClearAllRevertDataCommand"));
        Assert.Null(typeof(SettingsViewModel).GetProperty("OpenWebsiteCommand"));
        Assert.Null(typeof(SettingsViewModel).GetProperty("OpenLatestReleaseCommand"));
        Assert.Null(typeof(SettingsViewModel).GetProperty("ToggleRemoveProvisionedCommand"));
        Assert.Null(
            typeof(SettingsViewModel).GetProperty("ToggleShowCompletionNotificationCommand")
        );
        Assert.NotNull(typeof(SettingsViewModel).GetProperty("ToggleSmoothScrollingCommand"));
        Assert.NotNull(typeof(SettingsViewModel).GetProperty("OpenRootDirCommand"));
    }

    [Fact]
    public void Optimizer_AllCategoriesUseOnePage()
    {
        var appAssembly = typeof(OptimizePage).Assembly;
        var categoryPageTypes = typeof(IOptimizationCategory)
            .Assembly.GetTypes()
            .Where(type => typeof(IOptimizationCategory).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => type.GetCustomAttribute<OptimizationCategoryAttribute>()?.PageType)
            .Where(type => type != null)
            .Distinct()
            .ToArray();

        Assert.Equal([typeof(OptimizePage)], categoryPageTypes);
        Assert.Null(appAssembly.GetType("fasterLegit.UI.ViewModels.Pages.OptimizeViewModel"));
        Assert.Null(
            appAssembly.GetType("fasterLegit.UI.Pages.Optimize.Categories.PerformanceOptimizerPage")
        );
        Assert.Null(
            appAssembly.GetType(
                "fasterLegit.UI.Pages.Optimize.Categories.SecurityAndPrivacyOptimizerPage"
            )
        );
        Assert.Null(
            appAssembly.GetType(
                "fasterLegit.UI.Pages.Optimize.Categories.UserExperienceOptimizerPage"
            )
        );
        Assert.Null(
            appAssembly.GetType(
                "fasterLegit.UI.Pages.Optimize.Categories.BloatwareAndServicesOptimizerPage"
            )
        );
    }
}
