using System.Reflection;
using fasterLegit.Common.Helpers;
using fasterLegit.Domain.Abstractions;
using fasterLegit.Domain.Attributes;
using fasterLegit.Services.Customize;
using fasterLegit.Services.System;
using fasterLegit.UI.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fasterLegit.Common.Extensions;

/// <summary>
///     Shared plumbing for the reflection-discovered category pages. Kept in one place so
///     the optimization and customize paths cannot drift apart.
/// </summary>
/// <remarks>
///     A category page needs the <em>discovered category instance</em> (built by the
///     registry during preload, not by the container), so the page is registered against a
///     factory that pulls it from the registry. Everything else in the view model comes
///     from DI.
/// </remarks>
internal static class CategoryPageFactory
{
    /// <summary>Registers one singleton page per discovered category type.</summary>
    public static void AddCategoryPages<TCategory>(
        IServiceCollection services,
        Func<Type, Type?> resolvePageType,
        Func<IServiceProvider, Type, object> createViewModel
    )
        where TCategory : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(resolvePageType);
        ArgumentNullException.ThrowIfNull(createViewModel);

        foreach (
            var categoryType in ReflectionHelper.FindImplementationsInLoadedAssemblies<TCategory>()
        )
        {
            var pageType = resolvePageType(categoryType);
            if (pageType is null)
                continue;

            var capturedCategoryType = categoryType;
            services.AddSingleton(
                pageType,
                sp => Activator.CreateInstance(pageType, createViewModel(sp, capturedCategoryType))!
            );
        }
    }
}

/// <summary>
///     Registers the customize-category pages discovered on <see cref="ICustomizeCategory"/>.
/// </summary>
public static class CustomizePageRegistryExtensions
{
    public static void AddAllCustomizeCategoryPages(this IServiceCollection services)
    {
        CategoryPageFactory.AddCategoryPages<ICustomizeCategory>(
            services,
            categoryType => categoryType.GetCustomAttribute<CustomizeCategoryAttribute>()?.PageType,
            CreateViewModel
        );
    }

    private static object CreateViewModel(IServiceProvider sp, Type categoryType)
    {
        var registry = sp.GetRequiredService<CustomizeRegistry>();

        return new CustomizeCategoryViewModel(
            registry.Categories.First(c => c.GetType() == categoryType),
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<IRegistryWatcher>(),
            sp.GetRequiredService<SystemInfoService>()
        );
    }
}
