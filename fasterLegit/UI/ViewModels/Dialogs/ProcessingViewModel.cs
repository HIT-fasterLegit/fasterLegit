using CommunityToolkit.Mvvm.ComponentModel;
using fasterLegit.Common.Extensions;
using fasterLegit.Domain.UI;

namespace fasterLegit.UI.ViewModels.Dialogs;

public partial class ProcessingViewModel : LocalizedObject
{
    [ObservableProperty]
    private bool _isIndeterminate;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private int _total;

    [ObservableProperty]
    private int _value;

    public ProcessingViewModel()
    {
        ProgressReporter = new Progress<ProcessingProgress>(p =>
        {
            IsIndeterminate = p.IsIndeterminate;
            Message = p.Message;
            Value = p.Value;
            Total = p.Total;
        });
    }

    public IProgress<ProcessingProgress> ProgressReporter { get; }
}
