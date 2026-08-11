using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nizam.Desktop.Services;

namespace Nizam.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly NavigationService _navigation;

    public MainViewModel(NavigationService navigation)
    {
        _navigation = navigation;
        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new("dashboard", "Gösterge Paneli"),
            new("projects", "Projeler"),
            new("resources", "Kaynaklar"),
            new("reports", "Raporlar"),
            new("admin", "Yönetim")
        };
        StatusText = "Hazır";
        SelectedNavigationKey = "projects";
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    [ObservableProperty]
    private string _statusText = "Hazır";

    [ObservableProperty]
    private string _selectedNavigationKey = "projects";

    [ObservableProperty]
    private string _currentUserDisplay = string.Empty;

    partial void OnSelectedNavigationKeyChanged(string value)
    {
        _navigation.NavigateTo(value);
        StatusText = $"Görünüm: {NavigationItems.FirstOrDefault(i => i.Key == value)?.Title ?? value}";
    }

    [RelayCommand]
    private void Navigate(string key)
    {
        SelectedNavigationKey = key;
    }
}

public sealed record NavigationItem(string Key, string Title);
