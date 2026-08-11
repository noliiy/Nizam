using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nizam.Desktop.Models;
using Nizam.Desktop.Services;

namespace Nizam.Desktop.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly INizamApiClient _api;
    private readonly IDialogService _dialogs;

    public DashboardViewModel(INizamApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    [ObservableProperty]
    private Guid _projectId;

    [ObservableProperty]
    private ProjectDashboardDto? _dashboard;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public string TitleLabel => "Gösterge Paneli";
    public string PercentLabel => "Tamamlanma %";
    public string ForecastLabel => "Tahmini Bitiş";
    public string DataDateLabel => "Veri Tarihi";
    public string ActivityCountLabel => "Aktivite Sayısı";
    public string CriticalCountLabel => "Kritik Aktivite";
    public string CompletedCountLabel => "Tamamlanan";
    public string BaselineLabel => "Son Baseline";
    public string AvgVarianceLabel => "Ort. Bitiş Sapması (dk)";

    public void SetProject(Guid projectId) => ProjectId = projectId;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (ProjectId == Guid.Empty)
            return;

        try
        {
            IsBusy = true;
            Dashboard = await _api.GetDashboardAsync(ProjectId, cancellationToken);
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
