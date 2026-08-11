using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Nizam.Desktop.ViewModels;

public partial class ProjectWorkspaceViewModel : ObservableObject
{
    public ProjectWorkspaceViewModel()
    {
        Tabs = new ObservableCollection<WorkspaceTab>
        {
            new("overview", "Genel Bakış"),
            new("wbs", "WBS"),
            new("activities", "Aktiviteler"),
            new("gantt", "Gantt"),
            new("progress", "İlerleme"),
            new("baseline", "Baseline"),
            new("settings", "Ayarlar")
        };
        SelectedTabKey = "overview";
    }

    public ObservableCollection<WorkspaceTab> Tabs { get; }

    [ObservableProperty]
    private Guid _projectId;

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string _selectedTabKey = "overview";

    [ObservableProperty]
    private string _statusText = string.Empty;

    public void Initialize(Guid projectId, string projectName)
    {
        ProjectId = projectId;
        ProjectName = projectName;
        StatusText = $"Proje: {projectName}";
    }

    [RelayCommand]
    private void SelectTab(string key)
    {
        SelectedTabKey = key;
        StatusText = $"Sekme: {Tabs.FirstOrDefault(t => t.Key == key)?.Title ?? key}";
    }
}

public sealed record WorkspaceTab(string Key, string Title);
