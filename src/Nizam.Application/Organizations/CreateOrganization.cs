using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Application.Common;
using Nizam.Automation.Contracts.Events;
using Nizam.Domain.Entities;
using Nizam.Domain.Enums;
using Nizam.Domain.Security;

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
    private readonly ICurrentUser _currentUser;

    public CreateOrganizationHandler(IApplicationDbContext db, IDateTime clock, ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<OrganizationDto> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            throw new ForbiddenException("Kimlik doğrulama gerekli.");

        var now = _clock.UtcNow;
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Organizations.Add(org);
        _db.OrganizationMembers.Add(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            UserId = _currentUser.UserId.Value,
            RoleName = RoleNames.Administrator
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new OrganizationDto(org.Id, org.Name, org.CreatedAt);
    }
}
