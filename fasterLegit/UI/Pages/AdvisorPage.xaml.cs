using System.Windows;
using System.Windows.Controls;
using fasterLegit.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace fasterLegit.UI.Pages;

public partial class AdvisorPage : INavigableView<AdvisorViewModel>
{
    public AdvisorViewModel ViewModel { get; }

    public AdvisorPage(AdvisorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    private void OnApiKeyChanged(object sender, RoutedEventArgs e) =>
        ViewModel.NewApiKey = ((PasswordBox)sender).Password;

    private void OnSaveSettings(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveSettingsCommand.Execute(null);
        ApiKeyInput.Password = "";
    }
}
