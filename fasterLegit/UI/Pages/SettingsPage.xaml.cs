using fasterLegit.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace fasterLegit.UI.Pages;

public partial class SettingsPage : INavigableView<SettingsViewModel>
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }
}
