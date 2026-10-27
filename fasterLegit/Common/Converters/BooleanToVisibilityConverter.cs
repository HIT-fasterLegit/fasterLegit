using System.Windows;

namespace fasterLegit.Common.Converters;

public sealed class BooleanToVisibilityConverter()
    : BooleanConverter<Visibility>(Visibility.Visible, Visibility.Collapsed);
