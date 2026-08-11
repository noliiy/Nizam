namespace Nizam.Desktop.Services;

public interface IDialogService
{
    string ShowError(string message);
    string ShowInfo(string message);
}

/// <summary>Default implementation returns the formatted message for testability / non-UI hosts.</summary>
public sealed class DialogService : IDialogService
{
    public string LastMessage { get; private set; } = string.Empty;
    public string LastKind { get; private set; } = string.Empty;

    public string ShowError(string message)
    {
        LastKind = "Hata";
        LastMessage = message;
        return $"Hata: {message}";
    }

    public string ShowInfo(string message)
    {
        LastKind = "Bilgi";
        LastMessage = message;
        return $"Bilgi: {message}";
    }
}

public sealed class FakeDialogService : IDialogService
{
    public List<string> Errors { get; } = new();
    public List<string> Infos { get; } = new();

    public string ShowError(string message)
    {
        Errors.Add(message);
        return message;
    }

    public string ShowInfo(string message)
    {
        Infos.Add(message);
        return message;
    }
}
