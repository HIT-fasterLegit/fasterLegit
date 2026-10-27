using fasterLegit.Domain.Abstractions;
using fasterLegit.Domain.Execution;
using fasterLegit.Services.System;
using fasterLegit.Services.System.Primitives;
using Microsoft.Extensions.Logging;

namespace fasterLegit.Domain.Optimizations.Models;

/// <summary>
///     Per-apply call context: the explicit <see cref="OpCall"/> surface
///     (change collector, logger, cancellation) plus optimization-specific
///     dependencies (system snapshot, download service).
/// </summary>
public class OptimizationContext : OpCall
{
    /// <summary>Gets the system snapshot containing hardware, OS, and driver details.</summary>
    public required SystemInfo Snapshot { get; init; }

    /// <summary>
    ///     Gets the service used to download remote resources required by optimizations.
    /// </summary>
    public required StreamService StreamService { get; init; }

    /// <summary>
    ///     Gets the shell service used to execute CMD/PowerShell commands.
    /// </summary>
    public required ShellService Shell { get; init; }

    /// <summary>
    ///     Gets the native power-scheme service (get/set/import/delete, live names).
    /// </summary>
    public required PowerPlanService PowerPlans { get; init; }
}
