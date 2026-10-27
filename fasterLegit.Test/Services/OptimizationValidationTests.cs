using System.Globalization;
using System.Reflection;
using fasterLegit.Common.Helpers;
using fasterLegit.Domain.Abstractions;
using fasterLegit.Domain.Attributes;
using fasterLegit.Domain.Execution;
using fasterLegit.Domain.Optimizations.Categories;
using fasterLegit.Domain.Optimizations.Models;
using fasterLegit.Domain.UI;
using fasterLegit.Resources.Languages;
using fasterLegit.Services.Optimization;

namespace fasterLegit.Test.Services;

public class OptimizationValidationTests
{
    [Fact]
    public void Assemblies_ProjectRename_UsesFasterLegitNames()
    {
        Assert.Equal("fasterLegit", typeof(fasterLegit.App).Assembly.GetName().Name);
        Assert.Equal("fasterLegit.Test", GetType().Assembly.GetName().Name);
    }

    [Optimization(
        Id = "11111111-1111-1111-1111-111111111111",
        Risk = OptimizationRisk.Safe,
        Tags = OptimizationTags.System
    )]
    private sealed class ValidOptimization : BaseOptimization
    {
        public override Task<ApplyResult> ApplyAsync(
            IProgress<ProcessingProgress> progress,
            OptimizationContext context
        )
        {
            return Task.FromResult(ApplyResult.True());
        }
    }

    [Optimization(Id = "not-a-guid", Risk = OptimizationRisk.Safe, Tags = OptimizationTags.System)]
    private sealed class InvalidIdOptimization : BaseOptimization
    {
        public override Task<ApplyResult> ApplyAsync(
            IProgress<ProcessingProgress> progress,
            OptimizationContext context
        )
        {
            return Task.FromResult(ApplyResult.True());
        }
    }

    [Optimization(
        Id = "22222222-2222-2222-2222-222222222222",
        Risk = OptimizationRisk.Safe,
        Tags = OptimizationTags.System
    )]
    private sealed class DuplicateA : BaseOptimization
    {
        public override Task<ApplyResult> ApplyAsync(
            IProgress<ProcessingProgress> progress,
            OptimizationContext context
        )
        {
            return Task.FromResult(ApplyResult.True());
        }
    }

    [Optimization(
        Id = "22222222-2222-2222-2222-222222222222",
        Risk = OptimizationRisk.Safe,
        Tags = OptimizationTags.System
    )]
    private sealed class DuplicateB : BaseOptimization
    {
        public override Task<ApplyResult> ApplyAsync(
            IProgress<ProcessingProgress> progress,
            OptimizationContext context
        )
        {
            return Task.FromResult(ApplyResult.True());
        }
    }

    [Fact]
    public void Validate_ValidId_DoesNotThrow()
    {
        var seen = new HashSet<Guid>();
        var exception = Record.Exception(() =>
            OptimizationValidation.Validate(new ValidOptimization(), seen)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_InvalidId_ThrowsNamingTheClass()
    {
        var seen = new HashSet<Guid>();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            OptimizationValidation.Validate(new InvalidIdOptimization(), seen)
        );

        Assert.Contains(nameof(InvalidIdOptimization), ex.Message);
    }

    [Fact]
    public void Validate_DuplicateId_ThrowsNamingTheClass()
    {
        var seen = new HashSet<Guid>();
        OptimizationValidation.Validate(new DuplicateA(), seen);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            OptimizationValidation.Validate(new DuplicateB(), seen)
        );

        Assert.Contains(nameof(DuplicateB), ex.Message);
    }

    [Fact]
    public void Discovery_AllCatalogIds_AreValidAndUnique()
    {
        var seen = new HashSet<Guid>();
        var count = 0;

        foreach (
            var category in ReflectionHelper.FindImplementationsInLoadedAssemblies<IOptimizationCategory>()
        )
        {
            foreach (
                var nested in category
                    .GetNestedTypes(BindingFlags.Public)
                    .Where(t =>
                        typeof(IOptimization).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass
                    )
            )
            {
                var opt = (BaseOptimization)Activator.CreateInstance(nested)!;
                opt.OwnerType = category;
                OptimizationValidation.Validate(opt, seen);
                count++;
            }
        }

        Assert.True(count > 0, "No optimizations discovered.");
    }

    [Fact]
    public void Discovery_RemovedCategories_AreNotAvailable()
    {
        var categoryNames = ReflectionHelper
            .FindImplementationsInLoadedAssemblies<IOptimizationCategory>()
            .Select(category => category.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("AI", categoryNames);
        Assert.DoesNotContain("Gpu", categoryNames);
        Assert.DoesNotContain("PowerManagement", categoryNames);
    }

    [Fact]
    public void Discovery_OptimizationCatalog_ContainsOnlyRequestedOptimizations()
    {
        var optimizationNames = ReflectionHelper
            .FindImplementationsInLoadedAssemblies<IOptimizationCategory>()
            .SelectMany(category =>
                category
                    .GetNestedTypes(BindingFlags.Public)
                    .Where(type =>
                        typeof(IOptimization).IsAssignableFrom(type)
                        && type.IsClass
                        && !type.IsAbstract
                    )
            )
            .Select(type => type.FullName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                typeof(SecurityAndPrivacy.DisableAdvertisingAndSuggestions).FullName,
                typeof(UserExperience.DisableStartRecommended).FullName,
                typeof(UserExperience.DisableVisualEffects).FullName,
            ],
            optimizationNames
        );
    }

    [Fact]
    public void DisableVisualEffects_RegistryItems_DisablesWindowAnimations()
    {
        var item = UserExperience.DisableVisualEffects.RegistryItems.Single(item =>
            item.Name == "MinAnimate"
        );

        Assert.Equal(@"HKCU\Control Panel\Desktop\WindowMetrics", item.Path);
        Assert.Equal("0", item.Value);
    }

    [Fact]
    public void Discovery_AllCatalogResxKeys_Exist()
    {
        var missing = new List<string>();

        foreach (
            var category in ReflectionHelper.FindImplementationsInLoadedAssemblies<IOptimizationCategory>()
        )
        {
#if DEBUG
            // The Debug category carries hard-coded English, so it has no resource keys to find.
            if (category.Name == "Debug")
                continue;
#endif
            foreach (
                var nested in category
                    .GetNestedTypes(BindingFlags.Public)
                    .Where(t =>
                        typeof(IOptimization).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass
                    )
            )
            {
                var key = $"Optimizer.{category.Name}.{nested.Name}.Name";
                if (
                    Translations.ResourceManager.GetString(key, CultureInfo.InvariantCulture)
                    is null
                )
                    missing.Add(key);
            }
        }

        Assert.True(missing.Count == 0, "Missing resx keys: " + string.Join(", ", missing));
    }

    [Fact]
    public void Discovery_EveryTagAndEveryStepKind_ExplainsItself()
    {
        var missing = new List<string>();

        // A tag chip looks its explanation up by name, and the record looks up a label per kind of
        // step, so a value added without a key fails here instead of showing a bare key on screen.
        foreach (var tag in Enum.GetValues<OptimizationTags>())
        {
            if (tag == OptimizationTags.None)
                continue;

            var key = $"Optimizer.UI.Tags.{tag}.Tooltip";
            if (Translations.ResourceManager.GetString(key, CultureInfo.InvariantCulture) is null)
                missing.Add(key);
        }

        var labelKeys = Enum.GetValues<ChangeKind>()
            .Select(kind => kind.ToDisplay().LabelKey)
            .Append(ChangeKindPresentation.ForStep(new ChangeRecordStep { Ok = false }).LabelKey);

        foreach (var key in labelKeys)
        {
            if (Translations.ResourceManager.GetString(key, CultureInfo.InvariantCulture) is null)
                missing.Add(key);
        }

        Assert.True(missing.Count == 0, "Missing resx keys: " + string.Join(", ", missing));
    }
}
