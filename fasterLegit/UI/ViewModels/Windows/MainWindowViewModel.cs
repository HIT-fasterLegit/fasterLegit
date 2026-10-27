using fasterLegit.Common.Extensions;
using fasterLegit.Common.Helpers;

namespace fasterLegit.UI.ViewModels.Windows;

public class MainWindowViewModel : LocalizedObject
{
    /// <summary>
    ///     Gets the application version displayed in the title bar.
    /// </summary>
    public string Version => $"[v{Shared.FileVersion}]";
}
