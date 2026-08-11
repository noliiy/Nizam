using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nizam.Desktop.Models;
using Nizam.Desktop.Services;

namespace Nizam.Desktop.ViewModels;

public partial class VarianceRow : ObservableObject
{
    public string ActivityCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public double? StartVarianceDays { get; init; }
    public double? FinishVarianceDays { get; init; }

    public string StartVarianceDisplay => StartVarianceDays is null
        ? "—"
        : $"{StartVarianceDays.Value:0.##} gün";

    public string FinishVarianceDisplay => FinishVarianceDays is null
        ? "—"
        : $"{FinishVarianceDays.Value:0.##} gün";
}

public partial class BaselineViewModel : ObservableObject
{
    private readonly INizamApiClient _api;
    private readonly IDialogService _dialogs;

    public BaselineViewModel(INizamApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public ObservableCollection<VarianceRow> Variances { get; } = new();

    [ObservableProperty]
    private Guid _projectId;

    [ObservableProperty]
    private Guid? _baselineId;

    [ObservableProperty]
    private string _newBaselineName = string.Empty;

    [ObservableProperty]
    private string? _compareBaselineName;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public string TitleLabel => "Baseline";
    public string NameLabel => "Baseline Adı";
    public string CreateLabel => "Baseline Oluştur";
    public string CompareLabel => "Karşılaştır";
    public string StartVarianceLabel => "Başlangıç Sapması";
    public string FinishVarianceLabel => "Bitiş Sapması";
    public string VarianceDaysLabel => "Sapma (gün)";

    public void SetProject(Guid projectId) => ProjectId = projectId;

    [RelayCommand]
    private async Task CreateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NewBaselineName))
        {
            ErrorMessage = "Baseline adı zorunludur.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        try
        {
            IsBusy = true;
            var created = await _api.CreateBaselineAsync(ProjectId, new CreateBaselineRequest
            {
                Name = NewBaselineName.Trim(),
                BaselineType = 0
            }, cancellationToken);

            BaselineId = created.Id;
            _dialogs.ShowInfo($"Baseline oluşturuldu: {created.Name}");
            NewBaselineName = string.Empty;
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
    private async Task CompareAsync(CancellationToken cancellationToken)
    {
        if (BaselineId is null)
        {
            ErrorMessage = "Karşılaştırılacak baseline seçin veya oluşturun.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        try
        {
            IsBusy = true;
            var compare = await _api.CompareBaselineAsync(ProjectId, BaselineId.Value, cancellationToken);
            CompareBaselineName = compare.BaselineName;
            Variances.Clear();
            foreach (var v in compare.Variances)
            {
                Variances.Add(new VarianceRow
                {
                    ActivityCode = v.ActivityCode,
                    Name = v.Name,
                    StartVarianceDays = v.StartVarianceMinutes.HasValue
                        ? v.StartVarianceMinutes.Value / 480.0
                        : null,
                    FinishVarianceDays = v.FinishVarianceMinutes.HasValue
                        ? v.FinishVarianceMinutes.Value / 480.0
                        : null
                });
            }
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
