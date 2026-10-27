namespace fasterLegit.Domain.Execution;

/// <summary>
///     The subjects of the built-in tools. An identifier is a permanent record key, so changing one
///     orphans the records already written under it.
/// </summary>
public static class ToolSubjects
{
    /// <summary>
    ///     The startup manager's scheduled-task list. Its toggles are reported as its own runs, not
    ///     as the task manager's.
    /// </summary>
    public static readonly OperationSubject StartupManagerTasks = new(
        Guid.Parse("34E4A189-58FA-418A-AEFB-9D384C4D586B"),
        "StartupManagerTasks",
        "Startup manager task"
    );
}
