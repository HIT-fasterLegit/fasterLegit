using fasterLegit.Resources.Languages;
using fasterLegit.Services.Configuration;
using fasterLegit.Services.System;

namespace fasterLegit.Domain.Conditions;

/// <summary>Requires Windows 11 (build 22000 or later).</summary>
public sealed class Windows11Condition : ConditionBase
{
    public override ConditionResult Evaluate(SystemInfo snapshot) =>
        TryGetOsBuild(snapshot, out var build) && build >= WindowsBuilds.Windows11
            ? ConditionResult.Available
            : ConditionResult.Unsupported(
                () => Loc.Instance["Condition.Windows11.Title"],
                () => Loc.Instance["Condition.Windows11.Description"]
            );
}
