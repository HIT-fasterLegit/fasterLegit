using fasterLegit.UI.ViewModels.Optimizer;
using Wpf.Ui.Abstractions.Controls;

namespace fasterLegit.UI.Pages;

public partial class OptimizePage : INavigableView<OptimizationCategoryViewModel>
{
    public OptimizePage(OptimizationCategoryViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    public OptimizationCategoryViewModel ViewModel { get; }
}
