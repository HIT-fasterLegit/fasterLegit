using fasterLegit.UI.ViewModels.Pages;

namespace fasterLegit.UI.Pages.Customize.Categories;

public sealed class PreferencesFeatureCategory : CustomizeCategoryPage
{
    public PreferencesFeatureCategory(CustomizeCategoryViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
    }
}

public sealed class SystemFeatureCategory : CustomizeCategoryPage
{
    public SystemFeatureCategory(CustomizeCategoryViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
    }
}

public sealed class GamingFeatureCategory : CustomizeCategoryPage
{
    public GamingFeatureCategory(CustomizeCategoryViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
    }
}

public sealed class DesktopFeatureCategory : CustomizeCategoryPage
{
    public DesktopFeatureCategory(CustomizeCategoryViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
    }
}
