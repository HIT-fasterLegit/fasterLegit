namespace fasterLegit.Services.System;

/// <summary>
///     Defines a compile-time-safe token for cross-page data change events.
/// </summary>
/// <remarks>
///     Create a new event by declaring a stateless record that implements this interface:
///     <code language="csharp">public readonly record struct MyEvent : ICrossPageEvent;</code>
///     Use it with <see cref="CrossPageEventBus.NotifyDataChanged{T}"/> and
///     <see cref="CrossPageEventBus.HasPendingRefresh{T}"/>.
/// </remarks>
public interface ICrossPageEvent;

/// <summary>
///     Signals that bloatware packages were removed or may have changed externally.
/// </summary>
/// <remarks>
///     Fired by <see cref="fasterLegit.UI.ViewModels.Pages.BloatwareViewModel"/>
///     after removing packages.
///     Consumed by <see cref="fasterLegit.UI.ViewModels.Pages.StartupManagerViewModel"/>
///     (reloads tasks) and
///     <see cref="fasterLegit.UI.ViewModels.Pages.BloatwareViewModel"/> (full refresh on
///     re-navigation).
/// </remarks>
public readonly record struct BloatwareChanged : ICrossPageEvent;
