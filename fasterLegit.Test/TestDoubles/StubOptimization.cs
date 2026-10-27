using fasterLegit.Domain.Abstractions;
using fasterLegit.Domain.Conditions;
using fasterLegit.Domain.Optimizations.Models;
using fasterLegit.Domain.UI;

namespace fasterLegit.Test.TestDoubles;

public abstract class StubOptimization : IOptimization
{
    public virtual Guid Id { get; init; } = Guid.NewGuid();
    public virtual OptimizationRisk Risk { get; init; } = OptimizationRisk.Safe;
    public virtual string OptimizationKey { get; init; } = "TestOptimization";
    public virtual string Name { get; init; } = "Test Optimization";
    public virtual string ShortDescription { get; init; } = "Test optimization";
    public virtual OptimizationState State { get; set; } = new();
    public virtual Type? ConditionType { get; init; }
    public virtual ConditionResult ConditionResult { get; set; } = ConditionResult.Available;

    public abstract Task<ApplyResult> ApplyAsync(
        IProgress<ProcessingProgress> progress,
        OptimizationContext context
    );
}
