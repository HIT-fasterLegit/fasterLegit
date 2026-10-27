using System.Collections.ObjectModel;
using fasterLegit.Common.Extensions;
using fasterLegit.Domain.Abstractions;
using fasterLegit.Domain.Attributes;
using fasterLegit.Domain.Execution;
using fasterLegit.Domain.Optimizations.Models;
using fasterLegit.Domain.Optimizations.Models.Services;
using fasterLegit.Domain.UI;
using fasterLegit.Services.Configuration;
using fasterLegit.Services.System.Primitives;
using fasterLegit.UI.Pages;
using Microsoft.Extensions.Logging;

namespace fasterLegit.Domain.Optimizations.Categories;

[OptimizationCategory(typeof(OptimizePage))]
public class SecurityAndPrivacy : LocalizedObject, IOptimizationCategory
{
    public string Name => Loc.Instance[$"Optimizer.{nameof(SecurityAndPrivacy)}"];
    public OptimizationCategoryOrder Order { get; init; } =
        OptimizationCategoryOrder.SecurityAndPrivacy;
    public ObservableCollection<IOptimization> Optimizations { get; init; } = [];

    [Optimization(
        Id = "B1C2D3E4-F5A6-4B7C-8D9E-0F1A2B3C4D5E",
        Risk = OptimizationRisk.Safe,
        Tags = OptimizationTags.Privacy | OptimizationTags.System
    )]
    public class DisableAdvertisingAndSuggestions : BaseOptimization
    {
        public override Task<ApplyResult> ApplyAsync(
            IProgress<ProcessingProgress> progress,
            OptimizationContext context
        )
        {
            // @formatter:off
            RegistryService.Write(
                context,
                new RegistryItem(
                    @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                    "Enabled",
                    0
                ),
                new RegistryItem(
                    @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
                    "DisabledByGroupPolicy",
                    1
                ),
                new RegistryItem(
                    @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Privacy",
                    "TailoredExperiencesWithDiagnosticDataEnabled",
                    0
                ),
                new RegistryItem(
                    @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                    "DisableTailoredExperiencesWithDiagnosticData",
                    1
                ),
                new RegistryItem(
                    @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                    "DisableWindowsConsumerFeatures",
                    1
                ),
                new RegistryItem(
                    @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                    "DisableSoftLanding",
                    1
                ),
                new RegistryItem(
                    @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                    "DisableThirdPartySuggestions",
                    1
                ),
                new RegistryItem(
                    @"HKCU\Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement",
                    "ScoobeSystemSettingEnabled",
                    0
                ),
                new RegistryItem(
                    @"HKCU\SOFTWARE\Microsoft\InputPersonalization",
                    "RestrictImplicitInkCollection",
                    1
                ),
                new RegistryItem(
                    @"HKCU\SOFTWARE\Microsoft\InputPersonalization",
                    "RestrictImplicitTextCollection",
                    1
                ),
                new RegistryItem(
                    @"HKCU\SOFTWARE\Microsoft\InputPersonalization\TrainedDataStore",
                    "HarvestContacts",
                    0
                ),
                new RegistryItem(
                    @"HKCU\Control Panel\International\User Profile",
                    "HttpAcceptLanguageOptOut",
                    1
                )
            );
            // @formatter:on
            context.Logger.LogInformation(
                "Disabled advertising ID, consumer features and system suggestions"
            );

            return Task.FromResult(context.Changes.ToApplyResult());
        }
    }
}
