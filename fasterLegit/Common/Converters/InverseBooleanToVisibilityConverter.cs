using System.Windows;

namespace fasterLegit.Common.Converters;

public sealed class InverseBooleanToVisibilityConverter()
    : BooleanConverter<Visibility>(Visibility.Collapsed, Visibility.Visible);
