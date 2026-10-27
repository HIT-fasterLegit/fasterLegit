using System.Windows.Controls;
using StartupTask = fasterLegit.Domain.Optimizations.Models.StartupManager.StartupTask;

namespace fasterLegit.UI.Dialogs;

public partial class StartupTaskDetailsPanel : UserControl
{
    public StartupTaskDetailsPanel(StartupTask task)
    {
        InitializeComponent();
        DataContext = task;
    }
}
