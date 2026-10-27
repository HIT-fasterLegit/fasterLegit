using fasterLegit.Domain.Optimizations.Models.Services;
using fasterLegit.Domain.Revert.Steps;
using fasterLegit.Test.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace fasterLegit.Test.Domain.Revert.Steps;

public class ServiceRevertStepTests
{
    [Fact]
    public async Task ExecuteAsync_MissingService_ReturnsTrueWithoutThrowing()
    {
        // A service that no longer exists has nothing to restore: revert must
        // succeed instead of throwing on the unverifiable query.
        var step = new ServiceRevertStep
        {
            ServiceName = $"fasterLegit_Test_Missing_{Guid.NewGuid():N}",
            OriginalStartupType = ServiceStartupType.Manual,
        };

        var result = await step.ExecuteAsync(TestShell.Context(), NullLogger.Instance);

        Assert.True(result);
    }
}
