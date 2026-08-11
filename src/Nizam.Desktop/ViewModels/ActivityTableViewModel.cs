using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nizam.Desktop.Models;
using Nizam.Desktop.Services;

namespace Nizam.Desktop.ViewModels;

public partial class ActivityRow : ObservableObject
{
    public Guid Id { get; set; }
    public Guid WbsId { get; set; }

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private double _durationDays;
    [ObservableProperty] private DateTime? _start;
    [ObservableProperty] private DateTime? _finish;
    [ObservableProperty] private double _totalFloatDays;
    [ObservableProperty] private bool _isCritical;
    [ObservableProperty] private int _remainingDurationMinutes;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private decimal _percentComplete;

    public static ActivityRow FromDto(ActivityDto dto) => new()
    {
        Id = dto.Id,
        WbsId = dto.WbsId,
        Code = dto.ActivityCode,
        Name = dto.Name,
        DurationDays = dto.OriginalDurationMinutes / 480.0,
        Start = dto.EarlyStart,
        Finish = dto.EarlyFinish,
        TotalFloatDays = dto.TotalFloatMinutes / 480.0,
        IsCritical = dto.IsCritical,
        RemainingDurationMinutes = dto.RemainingDurationMinutes,
        Description = dto.Description,
        PercentComplete = dto.PercentComplete
    };
}

public partial class ActivityTableViewModel : ObservableObject
{
    private readonly INizamApiClient _api;
    private readonly IDialogService _dialogs;

    public ActivityTableViewModel(INizamApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public ObservableCollection<ActivityRow> Activities { get; } = new();

    [ObservableProperty]
    private Guid _projectId;

    [ObservableProperty]
    private Guid _defaultWbsId;

    [ObservableProperty]
    private ActivityRow? _selectedActivity;

    [ObservableProperty]
    private string _newCode = string.Empty;

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private double _newDurationDays = 1;

    [ObservableProperty]
    private Guid? _fsPredecessorId;

    [ObservableProperty]
    private Guid? _fsSuccessorId;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public string CodeColumn => "Kod";
    public string NameColumn => "Ad";
    public string DurationColumn => "Süre";
    public string StartColumn => "Başlangıç";
    public string FinishColumn => "Bitiş";
    public string FloatColumn => "Toplam Bolluk";
    public string CriticalColumn => "Kritik";
    public string RunScheduleLabel => "Zamanlamayı Çalıştır";
    public string NewActivityLabel => "Yeni Aktivite";
    public string SaveLabel => "Kaydet";
    public string DeleteLabel => "Sil";
    public string AddFsLabel => "FS İlişkisi Ekle";

    public void SetProject(Guid projectId, Guid defaultWbsId)
    {
        ProjectId = projectId;
        DefaultWbsId = defaultWbsId;
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (ProjectId == Guid.Empty)
            return;

        try
        {
            IsBusy = true;
            var list = await _api.GetActivitiesAsync(ProjectId, cancellationToken);
            Activities.Clear();
            foreach (var a in list)
                Activities.Add(ActivityRow.FromDto(a));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _dialogs.ShowError(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NewActivityAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NewCode) || string.IsNullOrWhiteSpace(NewName))
        {
            ErrorMessage = "Aktivite kodu ve adı zorunludur.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        if (DefaultWbsId == Guid.Empty)
        {
            ErrorMessage = "WBS seçilmedi.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        try
        {
            IsBusy = true;
            var minutes = (int)Math.Round(NewDurationDays * 480);
            var created = await _api.CreateActivityAsync(ProjectId, new CreateActivityRequest
            {
                WbsId = DefaultWbsId,
                ActivityCode = NewCode.Trim(),
                Name = NewName.Trim(),
                OriginalDurationMinutes = minutes
            }, cancellationToken);

            Activities.Add(ActivityRow.FromDto(created));
            NewCode = string.Empty;
            NewName = string.Empty;
            NewDurationDays = 1;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _dialogs.ShowError(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (SelectedActivity is null || SelectedActivity.Id == Guid.Empty)
        {
            ErrorMessage = "Kaydedilecek aktivite seçin.";
            return;
        }

        try
        {
            IsBusy = true;
            var minutes = (int)Math.Round(SelectedActivity.DurationDays * 480);
            var updated = await _api.UpdateActivityAsync(ProjectId, SelectedActivity.Id, new UpdateActivityRequest
            {
                Name = SelectedActivity.Name,
                Description = SelectedActivity.Description,
                OriginalDurationMinutes = minutes,
                RemainingDurationMinutes = SelectedActivity.RemainingDurationMinutes > 0
                    ? SelectedActivity.RemainingDurationMinutes
                    : minutes,
                WbsId = SelectedActivity.WbsId
            }, cancellationToken);

            var idx = Activities.IndexOf(SelectedActivity);
            if (idx >= 0)
                Activities[idx] = ActivityRow.FromDto(updated);
            _dialogs.ShowInfo("Aktivite kaydedildi.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _dialogs.ShowError(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedActivity is null)
        {
            ErrorMessage = "Silinecek aktivite seçin.";
            return;
        }

        // MVP: local remove; dedicated delete endpoint may follow.
        Activities.Remove(SelectedActivity);
        SelectedActivity = null;
        _dialogs.ShowInfo("Aktivite listeden kaldırıldı (yerel).");
    }

    [RelayCommand]
    private async Task RunScheduleAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            var result = await _api.RunScheduleAsync(ProjectId, cancellationToken);
            if (!string.IsNullOrEmpty(result.Error))
            {
                ErrorMessage = result.Error;
                _dialogs.ShowError(result.Error);
                return;
            }

            await LoadAsync(cancellationToken);
            _dialogs.ShowInfo("Zamanlama tamamlandı.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _dialogs.ShowError(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddFsRelationshipAsync(CancellationToken cancellationToken)
    {
        if (FsPredecessorId is null || FsSuccessorId is null)
        {
            ErrorMessage = "FS ilişkisi için öncül ve ardıl aktivite seçin.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        try
        {
            IsBusy = true;
            await _api.CreateRelationshipAsync(ProjectId, new CreateRelationshipRequest
            {
                PredecessorActivityId = FsPredecessorId.Value,
                SuccessorActivityId = FsSuccessorId.Value,
                RelationshipType = 0, // FS
                LagMinutes = 0
            }, cancellationToken);
            _dialogs.ShowInfo("FS ilişkisi eklendi.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _dialogs.ShowError(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
