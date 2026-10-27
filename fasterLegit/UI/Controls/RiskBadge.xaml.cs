using System.Windows;
using System.Windows.Controls;
using fasterLegit.Domain.UI;

namespace fasterLegit.UI.Controls;

public partial class RiskBadge : UserControl
{
    public static readonly DependencyProperty RiskProperty = DependencyProperty.Register(
        nameof(Risk),
        typeof(object),
        typeof(RiskBadge),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty RiskVisualProperty = DependencyProperty.Register(
        nameof(RiskVisual),
        typeof(RiskVisual),
        typeof(RiskBadge),
        new PropertyMetadata(null)
    );

    public RiskBadge()
    {
        InitializeComponent();
    }

    public object? Risk
    {
        get => GetValue(RiskProperty);
        set => SetValue(RiskProperty, value);
    }

    public RiskVisual? RiskVisual
    {
        get => (RiskVisual?)GetValue(RiskVisualProperty);
        set => SetValue(RiskVisualProperty, value);
    }
}
