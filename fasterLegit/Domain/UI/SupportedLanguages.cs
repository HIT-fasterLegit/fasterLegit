using System.Globalization;

namespace fasterLegit.Domain.UI;

/// <summary>Single source of truth for supported UI languages.</summary>
public static class SupportedLanguages
{
    public static IReadOnlyList<LanguageOption> All { get; } =
    [new() { DisplayName = "Tiếng Việt", Culture = new CultureInfo("vi-VN") }];

    public static LanguageOption Default => All[0];
}
