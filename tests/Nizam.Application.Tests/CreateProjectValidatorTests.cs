using FluentAssertions;
using FluentValidation.TestHelper;
using Nizam.Application.Projects;

namespace Nizam.Application.Tests;

public class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _validator = new();

    [Fact]
    public void Name_Required()
    {
        var result = _validator.TestValidate(new CreateProjectCommand(Guid.NewGuid(), "P1", "", null));
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Proje adı zorunludur.");
    }

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(new CreateProjectCommand(Guid.NewGuid(), "P1", "Proje", null));
        result.IsValid.Should().BeTrue();
    }
}
