using FluentValidation;
using MediatR;
using Nizam.Application.Abstractions;
using Nizam.Domain.Entities;

namespace Nizam.Application.Organizations;

public sealed record OrganizationDto(Guid Id, string Name, DateTime CreatedAt);

public sealed record CreateOrganizationCommand(string Name) : IRequest<OrganizationDto>;

public sealed class CreateOrganizationValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Organizasyon adı zorunludur.");
    }
}

public sealed class CreateOrganizationHandler : IRequestHandler<CreateOrganizationCommand, OrganizationDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public CreateOrganizationHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<OrganizationDto> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(cancellationToken);
        return new OrganizationDto(org.Id, org.Name, org.CreatedAt);
    }
}
