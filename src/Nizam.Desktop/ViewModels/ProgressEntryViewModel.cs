using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nizam.Desktop.Models;
using Nizam.Desktop.Services;

namespace Nizam.Desktop.ViewModels;

public partial class ProgressEntryViewModel : ObservableObject
{
    private readonly INizamApiClient _api;
    private readonly IDialogService _dialogs;

    public ProgressEntryViewModel(INizamApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    [ObservableProperty]
    private Guid _projectId;

    [ObservableProperty]
    private Guid _activityId;

    [ObservableProperty]
    private DateTime? _actualStart;

    [ObservableProperty]
    private DateTime? _actualFinish;

    [ObservableProperty]
    private decimal _percentComplete;

    [ObservableProperty]
    private int? _remainingDuration;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private string? _errorMessage;

    public string TitleLabel => "İlerleme Girişi";
    public string ActualStartLabel => "Gerçek Başlangıç";
    public string ActualFinishLabel => "Gerçek Bitiş";
    public string PercentLabel => "Tamamlanma %";
    public string RemainingLabel => "Kalan Süre (dk)";
    public string SubmitLabel => "Gönder";

    public void SetContext(Guid projectId, Guid activityId)
    {
        ProjectId = projectId;
        ActivityId = activityId;
    }

    [RelayCommand]
    private async Task SubmitAsync(CancellationToken cancellationToken)
    {
        if (ActivityId == Guid.Empty)
        {
            ErrorMessage = "Aktivite seçilmedi.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        if (PercentComplete < 0 || PercentComplete > 100)
        {
            ErrorMessage = "İlerleme yüzdesi 0 ile 100 arasında olmalıdır.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        try
        {
            await _api.CreateProgressAsync(ProjectId, new CreateProgressRequest
            {
                ActivityId = ActivityId,
                ActualStart = ActualStart,
                ActualFinish = ActualFinish,
                PercentComplete = PercentComplete,
                RemainingDurationMinutes = RemainingDuration,
                Notes = Notes
            }, cancellationToken);

            _dialogs.ShowInfo("İlerleme kaydedildi.");
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _dialogs.ShowError(ErrorMessage);
        }
    }
}
