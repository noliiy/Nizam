using FluentAssertions;
using Nizam.Desktop.Services;
using Nizam.Desktop.ViewModels;

namespace Nizam.Desktop.Tests;

public class LoginViewModelTests
{
    private readonly FakeNizamApiClient _api = new();
    private readonly NavigationService _nav = new();
    private readonly FakeDialogService _dialogs = new();

    private LoginViewModel CreateSut() => new(_api, _nav, _dialogs);

    [Fact]
    public async Task Login_Requires_Email()
    {
        var vm = CreateSut();
        vm.Email = "";
        vm.Password = "x";

        await vm.LoginCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("E-posta zorunludur.");
        _dialogs.Errors.Should().ContainSingle(e => e == "E-posta zorunludur.");
        _api.LoginCalls.Should().Be(0);
    }

    [Fact]
    public async Task Login_Requires_Password()
    {
        var vm = CreateSut();
        vm.Email = "admin@nizam.local";
        vm.Password = "  ";

        await vm.LoginCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Şifre zorunludur.");
        _api.LoginCalls.Should().Be(0);
    }

    [Fact]
    public async Task Login_Requires_Valid_Email_Format()
    {
        var vm = CreateSut();
        vm.Email = "not-an-email";
        vm.Password = "Admin123!";

        await vm.LoginCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Geçerli bir e-posta adresi girin.");
        _api.LoginCalls.Should().Be(0);
    }

    [Fact]
    public async Task Login_Success_Navigates_To_Projects()
    {
        var vm = CreateSut();
        vm.Email = "admin@nizam.local";
        vm.Password = "Admin123!";

        await vm.LoginCommand.ExecuteAsync(null);

        _api.LoginCalls.Should().Be(1);
        _nav.CurrentViewKey.Should().Be("projects");
        vm.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Default_Email_Is_Demo_Admin()
    {
        var vm = CreateSut();
        vm.Email.Should().Be("admin@nizam.local");
    }
}
