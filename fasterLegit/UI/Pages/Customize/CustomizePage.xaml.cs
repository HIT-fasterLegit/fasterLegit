using fasterLegit.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace fasterLegit.UI.Pages;

public partial class CustomizePage : INavigableView<CustomizeViewModel>
{
    public CustomizePage(CustomizeViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    public CustomizeViewModel ViewModel { get; }
}
