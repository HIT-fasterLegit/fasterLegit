using System.Collections.ObjectModel;
using fasterLegit.Common.Helpers;
using fasterLegit.Domain.Abstractions;
using fasterLegit.Domain.Conditions;
using fasterLegit.Domain.Optimizations.Models;
using Microsoft.Extensions.Logging;

namespace fasterLegit.Services.Optimization;

public class OptimizationRegistry(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<OptimizationRegistry>();

    /// <summary>Gets or sets the optimization categories discovered by preloading.</summary>
    public IOptimizationCategory[] OptimizationCategories { get; set; } = [];

    /// <summary>
    ///     Gets a value that indicates whether the optimizations have been fully discovered and
    ///     their applied states loaded.
    /// </summary>
    public bool IsPreloaded { get; private set; }

    /// <summary>
    ///     Ensures categories and applied-state are loaded before the optimize UI binds.
    ///     Concurrent callers share a single discovery; a failed discovery is retried.
    /// </summary>
    public async Task EnsurePreloadedAsync()
    {
        if (IsPreloaded)
            return;

        _preloadTask ??= PreloadOptimizationsAsync();
        try
        {
            await _preloadTask.ConfigureAwait(false);
        }
        finally
        {
            if (!IsPreloaded)
                _preloadTask = null;
        }
    }

    private Task? _preloadTask;

    /// <summary>
    ///     Discovers the optimization categories by reflection and loads the applied state of
    ///     their optimizations from revert data on disk.
    /// </summary>
    public async Task PreloadOptimizationsAsync()
    {
        var seenIds = new HashSet<Guid>();
        // Run reflection work on background thread to avoid blocking startup
        var optimizationCategories = await Task.Run(() =>
                CategoryDiscovery.Discover<IOptimizationCategory, IOptimization>(
                    nameof(IOptimizationCategory.Optimizations),
                    t =>
                        CategoryDiscovery.NestedItems<IOptimization>(
                            t,
                            (opt, owner) =>
                            {
                                if (opt is BaseOptimization bo)
                                {
                                    bo.OwnerType = owner;
                                    OptimizationValidation.Validate(bo, seenIds);
                                    ConditionValidation.Validate(
                                        bo.ConditionType,
                                        bo.OptimizationKey
                                    );
                                }
                            }
                        ),
                    c => (int)c.Order
                )
            )
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Total {CategoryCount} categories and {OptimizationCount} optimizations found",
            optimizationCategories.Length,
            optimizationCategories.Sum(c => c.Optimizations.Count)
        );

        await OptimizationService
            .UpdateOptimizationStateAsync(optimizationCategories.SelectMany(c => c.Optimizations))
            .ConfigureAwait(false);

        OptimizationCategories = optimizationCategories;
        IsPreloaded = true;
    }
}
