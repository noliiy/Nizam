namespace Nizam.Desktop.Services;

public sealed class NavigationService
{
    public string CurrentViewKey { get; private set; } = "Login";

    public event EventHandler? CurrentViewChanged;

    public void NavigateTo(string viewKey)
    {
        if (string.IsNullOrWhiteSpace(viewKey))
            return;

        CurrentViewKey = viewKey;
        CurrentViewChanged?.Invoke(this, EventArgs.Empty);
    }
}
