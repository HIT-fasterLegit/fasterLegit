using Wpf.Ui.Controls;

namespace fasterLegit.Domain.UI;

/// <summary>
///     Visual display data for a risk level in the UI.
/// </summary>
public class RiskVisual
{
    public string Display { get; init; } = string.Empty;

    public SymbolRegular Icon { get; init; }
}
