using System.Globalization;
using fasterLegit.Resources.Languages;

namespace fasterLegit.Test.UI;

public class BrandLocalizationTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("vi-VN")]
    public void BrandCopy_SupportedPresentationCultures_HasCompleteIdentity(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        Assert.Equal("VELORA", Translations.ResourceManager.GetString("Brand.Name", culture));

        foreach (
            var key in new[]
            {
                "Brand.Tagline",
                "Brand.Overview.Title",
                "Brand.Overview.Description",
                "Brand.Optimize.Title",
                "Brand.Optimize.Description",
                "Sidebar.Dashboard",
                "Sidebar.Optimize",
                "Sidebar.Bloatware",
                "Sidebar.StartupManager",
                "Sidebar.DiskCleanup",
            }
        )
        {
            var value = Translations.ResourceManager.GetString(key, culture);
            Assert.False(string.IsNullOrWhiteSpace(value), key);
            Assert.DoesNotContain("fasterLegit", value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
