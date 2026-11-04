using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fasterLegit.Domain.Advisor;
using fasterLegit.Services.Advisor;
using fasterLegit.Services.Configuration;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace fasterLegit.UI.ViewModels.Pages;

public partial class AdvisorViewModel(
    AdvisorSettingsStore settings,
    AdvisorService advisor,
    IAdvisorModelClient model,
    IAdvisorEvidenceCollector collector,
    AdvisorActionExecutor executor,
    AdvisorReportStore reports,
    IContentDialogService dialogs
) : ViewModel
{
    private CancellationTokenSource? _cancellation;
    private AdvisorReport? _report;
    private AdvisorComparison? _comparison;
    private readonly List<string> _conversation = [];
    private readonly List<AdvisorExecutionResult> _executions = [];
    private AdvisorSettings _savedSettings = new();
    public ObservableCollection<AdvisorFinding> Findings { get; } = [];
    public ObservableCollection<AdvisorEvidence> Evidence { get; } = [];
    public ObservableCollection<AdvisorActionChoice> Actions { get; } = [];
    public ObservableCollection<string> ExecutionMessages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    private string _question = "";

    [ObservableProperty]
    private string _goal = "";

    [ObservableProperty]
    private int _modeIndex;

    [ObservableProperty]
    private int _providerIndex;

    [ObservableProperty]
    private string _localEndpoint = "http://127.0.0.1:11434";

    [ObservableProperty]
    private string _localModel = "qwen3:4b";

    [ObservableProperty]
    private string _apiEndpoint = "";

    [ObservableProperty]
    private string _apiModel = "";

    [ObservableProperty]
    private int _sampleSeconds = 15;

    [ObservableProperty]
    private bool _allowRemoteData;

    [ObservableProperty]
    private bool _hasStoredKey;

    [ObservableProperty]
    private string _summary = "";

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private string _error = "";

    [ObservableProperty]
    private string _comparisonText = "";

    [ObservableProperty]
    private string _savedReportPath = "";

    [ObservableProperty]
    private bool _hasReport;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveSettingsCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteActionsCommand))]
    [NotifyCanExecuteChangedFor(nameof(RecheckCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveReportCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearConversationCommand))]
    private bool _isBusy;
    public bool IsIdle => !IsBusy;
    public string NewApiKey { get; set; } = "";

    protected override Task InitializeOnceAsync()
    {
        try
        {
            _savedSettings = settings.Load();
            ProviderIndex = (int)_savedSettings.Provider;
            LocalEndpoint = _savedSettings.LocalEndpoint;
            LocalModel = _savedSettings.LocalModel;
            ApiEndpoint = _savedSettings.ApiEndpoint;
            ApiModel = _savedSettings.ApiModel;
            SampleSeconds = _savedSettings.SampleSeconds;
            HasStoredKey = settings.HasKey;
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        return Task.CompletedTask;
    }

    partial void OnProviderIndexChanged(int value) => AllowRemoteData = false;

    private bool CanAnalyze() => IsIdle && !string.IsNullOrWhiteSpace(Question);

    private bool CanExecuteActions() => IsIdle && _report != null && Actions.Any(x => x.IsSelected);

    private bool CanRecheck() => IsIdle && _report?.Measurement != null;

    private bool CanSaveReport() => IsIdle && _report != null;

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void SaveSettings()
    {
        try
        {
            var value = new AdvisorSettings
            {
                Provider = (AdvisorProvider)ProviderIndex,
                LocalEndpoint = LocalEndpoint.Trim(),
                LocalModel = LocalModel.Trim(),
                ApiEndpoint = ApiEndpoint.Trim(),
                ApiModel = ApiModel.Trim(),
                SampleSeconds = SampleSeconds,
            };
            settings.Save(value, NewApiKey);
            _savedSettings = value;
            NewApiKey = "";
            HasStoredKey = settings.HasKey;
            AllowRemoteData = false;
            Status = Loc.Instance["AI.Settings.Saved"];
            Error = "";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void ClearKey()
    {
        try
        {
            settings.ClearKey();
            HasStoredKey = false;
            NewApiKey = "";
            Status = Loc.Instance["AI.Settings.KeyCleared"];
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task TestConnectionAsync()
    {
        Begin();
        try
        {
            RequireSavedConfiguration();
            await model.CompleteAsync(
                "Return only JSON: {\"ok\":true}. This is a connection test; no machine data is attached.",
                "connection test",
                _cancellation!.Token
            );
            Status = Loc.Instance["AI.Settings.Connected", _savedSettings.Model];
        }
        catch (OperationCanceledException) when (_cancellation?.IsCancellationRequested == true)
        {
            Status = Loc.Instance["AI.Status.Cancelled"];
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            End();
        }
    }

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        Begin();
        try
        {
            RequireSavedConfiguration();
            _report = null;
            HasReport = false;
            foreach (var old in Actions)
                old.PropertyChanged -= OnActionSelectionChanged;
            Actions.Clear();
            Findings.Clear();
            Evidence.Clear();
            var question = Question.Trim();
            var request = new AdvisorRequest(
                question,
                Goal.Trim(),
                (AdvisorMode)ModeIndex,
                AllowRemoteData,
                _conversation.ToArray()
            );
            var report = await advisor.AnalyzeAsync(
                request,
                new Progress<string>(s => Status = s),
                _cancellation!.Token
            );
            _report = report;
            _comparison = null;
            _executions.Clear();
            ExecutionMessages.Clear();
            ComparisonText = "";
            SavedReportPath = "";
            Summary = string.IsNullOrWhiteSpace(report.FollowUpQuestion)
                ? report.Summary
                : report.FollowUpQuestion;
            Findings.Clear();
            foreach (var finding in report.Findings)
                Findings.Add(finding);
            Evidence.Clear();
            foreach (var evidence in report.Evidence)
                Evidence.Add(evidence);
            foreach (var old in Actions)
                old.PropertyChanged -= OnActionSelectionChanged;
            Actions.Clear();
            foreach (var action in report.Actions)
            {
                var choice = new AdvisorActionChoice(action);
                choice.PropertyChanged += OnActionSelectionChanged;
                Actions.Add(choice);
            }
            _conversation.Add("User: " + question);
            _conversation.Add("Advisor: " + Summary);
            while (_conversation.Count > 6)
                _conversation.RemoveAt(0);
            HasReport = true;
            Status = Loc.Instance["AI.Status.Done", report.Model, report.ToolsUsed.Count];
        }
        catch (OperationCanceledException) when (_cancellation?.IsCancellationRequested == true)
        {
            Status = Loc.Instance["AI.Status.Cancelled"];
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            End();
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private async Task ExecuteActionsAsync()
    {
        if (_report == null)
            return;
        var report = _report;
        var selected = Actions.Where(x => x.IsSelected).Select(x => x.Action.Id).ToArray();
        Begin();
        try
        {
            var selectedActions = AdvisorActionExecutor.ValidateSelection(
                report,
                selected,
                DateTimeOffset.UtcNow
            );
            var dialog = new ContentDialog
            {
                Title = Loc.Instance["AI.Confirm.Title"],
                Content =
                    Loc.Instance["AI.Confirm.Body"]
                    + "\n\n"
                    + string.Join(
                        "\n\n",
                        selectedActions.Select(a => a.Title + "\n" + a.Description)
                    ),
                PrimaryButtonText = Loc.Instance["AI.Confirm.Apply"],
                CloseButtonText = Loc.Instance["Common.Cancel"],
                DefaultButton = ContentDialogButton.Close,
            };
            var confirmed = await dialogs.ShowAsync(dialog, _cancellation!.Token);
            if (confirmed != ContentDialogResult.Primary)
                return;
            Status = Loc.Instance["AI.Progress.Execute"];
            var result = await executor.ExecuteAsync(
                report,
                selected,
                _cancellation.Token,
                new Progress<AdvisorExecutionResult>(item =>
                {
                    _executions.Add(item);
                    ExecutionMessages.Add(item.Title + ": " + item.Outcome);
                })
            );
            foreach (var action in Actions)
                action.IsSelected = false;
            Status = Loc.Instance[
                "AI.Execution.Completed",
                result.Count(x => x.Succeeded),
                result.Count
            ];
        }
        catch (OperationCanceledException) when (_cancellation?.IsCancellationRequested == true)
        {
            Status = Loc.Instance["AI.Status.Cancelled"];
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            End();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRecheck))]
    private async Task RecheckAsync()
    {
        if (_report?.Measurement is not { } before)
            return;
        Begin();
        try
        {
            Status = Loc.Instance["AI.Progress.Recheck"];
            var after = await collector.MeasureAsync(
                Math.Clamp(before.DurationSeconds, 5, 60),
                _cancellation!.Token
            );
            _comparison = AdvisorService.Compare(before, after);
            static string Cpu(double? value) =>
                value.HasValue ? $"{value:0.0}%" : Loc.Instance["Common.Unknown"];
            ComparisonText = Loc.Instance[
                "AI.Comparison",
                Cpu(_comparison.CpuBefore),
                Cpu(_comparison.CpuAfter),
                $"{_comparison.MemoryBefore:0.0}%",
                $"{_comparison.MemoryAfter:0.0}%",
                _comparison.SecondsBefore,
                _comparison.SecondsAfter
            ];
            Status = Loc.Instance["AI.Status.Rechecked"];
        }
        catch (OperationCanceledException) when (_cancellation?.IsCancellationRequested == true)
        {
            Status = Loc.Instance["AI.Status.Cancelled"];
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            End();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveReport))]
    private void SaveReport()
    {
        try
        {
            SavedReportPath = reports.Save(_report!, _executions, _comparison);
            Status = Loc.Instance["AI.Status.SavedReport"];
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void ClearConversation()
    {
        _conversation.Clear();
        Question = "";
        Goal = "";
        _report = null;
        _comparison = null;
        foreach (var action in Actions)
            action.PropertyChanged -= OnActionSelectionChanged;
        Actions.Clear();
        Evidence.Clear();
        Findings.Clear();
        ExecutionMessages.Clear();
        _executions.Clear();
        Summary = "";
        Error = "";
        Status = "";
        HasReport = false;
        ComparisonText = "";
        SavedReportPath = "";
        RefreshCommands();
    }

    [RelayCommand]
    private void Cancel() => _cancellation?.Cancel();

    private void OnActionSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AdvisorActionChoice.IsSelected))
            ExecuteActionsCommand.NotifyCanExecuteChanged();
    }

    private void Begin()
    {
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        IsBusy = true;
        Error = "";
    }

    private void End()
    {
        IsBusy = false;
        RefreshCommands();
    }

    private void RefreshCommands()
    {
        ExecuteActionsCommand.NotifyCanExecuteChanged();
        RecheckCommand.NotifyCanExecuteChanged();
        SaveReportCommand.NotifyCanExecuteChanged();
    }

    private void ShowError(Exception exception)
    {
        var key = exception.Message.Split(':')[0];
        Error = key.StartsWith("AI.Error.", StringComparison.Ordinal)
            ? Loc.Instance[key]
            : Loc.Instance["AI.Error.General"];
        if (exception.Message.StartsWith("AI.Error.Http:", StringComparison.Ordinal))
            Error += " (HTTP " + exception.Message.Split(':').Last() + ")";
    }

    private void RequireSavedConfiguration()
    {
        if (
            ProviderIndex != (int)_savedSettings.Provider
            || LocalEndpoint.Trim() != _savedSettings.LocalEndpoint
            || LocalModel.Trim() != _savedSettings.LocalModel
            || ApiEndpoint.Trim() != _savedSettings.ApiEndpoint
            || ApiModel.Trim() != _savedSettings.ApiModel
            || SampleSeconds != _savedSettings.SampleSeconds
            || !string.IsNullOrEmpty(NewApiKey)
        )
            throw new InvalidOperationException("AI.Error.Unsaved");
    }
}
