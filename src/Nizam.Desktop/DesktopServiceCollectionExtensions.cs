using Microsoft.Extensions.DependencyInjection;
using Nizam.Desktop.Services;
using Nizam.Desktop.ViewModels;

namespace Nizam.Desktop;

public static class DesktopServiceCollectionExtensions
{
    public static IServiceCollection AddNizamDesktop(this IServiceCollection services, Uri? apiBaseAddress = null)
    {
        services.AddSingleton<AuthenticationService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddHttpClient<INizamApiClient, ApiClient>(client =>
        {
            client.BaseAddress = apiBaseAddress ?? new Uri("http://localhost:5169/");
        });

        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ProjectListViewModel>();
        services.AddTransient<ProjectWorkspaceViewModel>();
        services.AddTransient<WbsTreeViewModel>();
        services.AddTransient<ActivityTableViewModel>();
        services.AddTransient<GanttViewModel>();
        services.AddTransient<ProgressEntryViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<BaselineViewModel>();

        return services;
    }
}
