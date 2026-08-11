using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nizam.Desktop.Services;

namespace Nizam.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly INizamApiClient _api;
    private readonly NavigationService _navigation;
    private readonly IDialogService _dialogs;

    public LoginViewModel(INizamApiClient api, NavigationService navigation, IDialogService dialogs)
    {
        _api = api;
        _navigation = navigation;
        _dialogs = dialogs;
        Email = "admin@nizam.local";
    }

    [ObservableProperty]
    private string _email = "admin@nizam.local";

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public string TitleLabel => "Nizam'a Giriş";
    public string EmailLabel => "E-posta";
    public string PasswordLabel => "Şifre";
    public string LoginButtonLabel => "Giriş Yap";

    [RelayCommand]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "E-posta zorunludur.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Şifre zorunludur.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        if (!Email.Contains('@'))
        {
            ErrorMessage = "Geçerli bir e-posta adresi girin.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        try
        {
            IsBusy = true;
            await _api.LoginAsync(Email.Trim(), Password, cancellationToken);
            _navigation.NavigateTo("projects");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message.Contains("Giriş", StringComparison.Ordinal)
                ? ex.Message
                : "Giriş başarısız. Lütfen bilgilerinizi kontrol edin.";
            _dialogs.ShowError(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
