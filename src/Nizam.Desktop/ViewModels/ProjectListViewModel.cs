using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nizam.Desktop.Models;
using Nizam.Desktop.Services;

namespace Nizam.Desktop.ViewModels;

public partial class ProjectListViewModel : ObservableObject
{
    private readonly INizamApiClient _api;
    private readonly AuthenticationService _auth;
    private readonly NavigationService _navigation;
    private readonly IDialogService _dialogs;

    public ProjectListViewModel(
        INizamApiClient api,
        AuthenticationService auth,
        NavigationService navigation,
        IDialogService dialogs)
    {
        _api = api;
        _auth = auth;
        _navigation = navigation;
        _dialogs = dialogs;
    }

    public ObservableCollection<ProjectDto> Projects { get; } = new();

    [ObservableProperty]
    private string _newProjectName = string.Empty;

    [ObservableProperty]
    private string _newProjectCode = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public string TitleLabel => "Projeler";
    public string NameLabel => "Ad";
    public string CodeLabel => "Kod";
    public string NewProjectButtonLabel => "Yeni Proje";

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_auth.OrganizationId is null)
        {
            ErrorMessage = "Organizasyon bulunamadı. Lütfen yeniden giriş yapın.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var list = await _api.GetProjectsAsync(_auth.OrganizationId.Value, cancellationToken);
            Projects.Clear();
            foreach (var p in list)
                Projects.Add(p);
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
    private async Task NewProjectAsync(CancellationToken cancellationToken)
    {
        if (_auth.OrganizationId is null)
        {
            ErrorMessage = "Organizasyon bulunamadı.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewProjectName))
        {
            ErrorMessage = "Proje adı zorunludur.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        if (string.IsNullOrWhiteSpace(NewProjectCode))
        {
            ErrorMessage = "Proje kodu zorunludur.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        try
        {
            IsBusy = true;
            var created = await _api.CreateProjectAsync(new CreateProjectRequest
            {
                OrganizationId = _auth.OrganizationId.Value,
                Name = NewProjectName.Trim(),
                ProjectCode = NewProjectCode.Trim()
            }, cancellationToken);

            Projects.Add(created);
            NewProjectName = string.Empty;
            NewProjectCode = string.Empty;
            _dialogs.ShowInfo($"Proje oluşturuldu: {created.Name}");
            _navigation.NavigateTo($"project:{created.Id}");
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
    private void OpenProject(ProjectDto? project)
    {
        if (project is null)
            return;
        _navigation.NavigateTo($"project:{project.Id}");
    }
}
