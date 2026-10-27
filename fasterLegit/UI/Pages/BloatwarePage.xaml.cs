using fasterLegit.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace fasterLegit.UI.Pages;

public partial class BloatwarePage : INavigableView<BloatwareViewModel>
{
    public BloatwarePage(BloatwareViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    public BloatwareViewModel ViewModel { get; }
}
