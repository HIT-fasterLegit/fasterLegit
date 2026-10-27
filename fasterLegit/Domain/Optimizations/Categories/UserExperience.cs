using System.Collections.ObjectModel;
using fasterLegit.Common.Extensions;
using fasterLegit.Domain.Abstractions;
using fasterLegit.Domain.Attributes;
using fasterLegit.Domain.Conditions;
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
public class UserExperience : LocalizedObject, IOptimizationCategory
{
    public string Name => Loc.Instance[$"Optimizer.{nameof(UserExperience)}"];
    public OptimizationCategoryOrder Order { get; init; } =
        OptimizationCategoryOrder.UserExperience;
    public ObservableCollection<IOptimization> Optimizations { get; init; } = [];

    [Optimization(
        Id = "A3D1E8F2-7B4C-4A5D-9E62-8F3A1B7C4D59",
        Risk = OptimizationRisk.Safe,
        Tags = OptimizationTags.System | OptimizationTags.Performance | OptimizationTags.Visual
    )]
    public sealed class DisableVisualEffects : BaseOptimization
    {
        internal static RegistryItem[] RegistryItems =>
            [
                new RegistryItem(
                    @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "TaskbarAnimations",
                    0
                ),
                new RegistryItem(
                    @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "ListviewShadow",
                    0
                ),
                new RegistryItem(
                    @"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "EnableTransparency",
                    0
                ),
                new RegistryItem(@"HKCU\Software\Microsoft\Windows\DWM", "EnableAeroPeek", 0),
                new RegistryItem(@"HKCU\Control Panel\Desktop\WindowMetrics", "MinAnimate", "0"),
            ];

        public override Task<ApplyResult> ApplyAsync(
            IProgress<ProcessingProgress> progress,
            OptimizationContext context
        )
        {
            RegistryService.Write(context, RegistryItems);
            context.Logger.LogInformation("Disabled visual effects for better performance");
            return Task.FromResult(context.Changes.ToApplyResult());
        }
    }

    [Optimization(
        Id = "31A6274C-AD37-4FFA-A32E-727FB71AD085",
        Risk = OptimizationRisk.Safe,
        Tags = OptimizationTags.Privacy | OptimizationTags.Visual | OptimizationTags.Windows11Only,
        Condition = typeof(Windows11Condition)
    )]
    public sealed class DisableStartRecommended : BaseOptimization
    {
        public override Task<ApplyResult> ApplyAsync(
            IProgress<ProcessingProgress> progress,
            OptimizationContext context
        )
        {
            RegistryService.Write(
                context,
                new RegistryItem(
                    @"HKEY_CURRENT_USER\SOFTWARE\Policies\Microsoft\Windows\Explorer",
                    "HideRecommendedSection",
                    1
                )
            );
            context.Logger.LogInformation("Disabled Recommended section in Start Menu");
            return Task.FromResult(context.Changes.ToApplyResult());
        }
    }
}
