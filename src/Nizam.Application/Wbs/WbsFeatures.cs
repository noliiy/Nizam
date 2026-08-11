using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Application.Common;
using Nizam.Domain.Entities;

namespace Nizam.Application.Wbs;

public sealed record WbsNodeDto(
    Guid Id,
    Guid ProjectId,
    Guid? ParentWbsId,
    string WbsCode,
    string Name,
    string? Description,
    int SortOrder,
    decimal Progress);

public sealed record CreateWbsNodeCommand(
    Guid ProjectId,
    Guid? ParentWbsId,
    string WbsCode,
    string Name,
    string? Description,
    int SortOrder = 0) : IRequest<WbsNodeDto>;

public sealed class CreateWbsNodeValidator : AbstractValidator<CreateWbsNodeCommand>
{
    public CreateWbsNodeValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Proje zorunludur.");
        RuleFor(x => x.WbsCode).NotEmpty().WithMessage("WBS kodu zorunludur.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("WBS adı zorunludur.");
    }
}

public sealed class CreateWbsNodeHandler : IRequestHandler<CreateWbsNodeCommand, WbsNodeDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public CreateWbsNodeHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<WbsNodeDto> Handle(CreateWbsNodeCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Proje bulunamadı.");

        var exists = await _db.WbsNodes.AnyAsync(
            w => w.ProjectId == request.ProjectId && w.WbsCode == request.WbsCode,
            cancellationToken);
        if (exists)
            throw new ConflictException("Bu WBS kodu zaten kullanılıyor.");

        if (request.ParentWbsId is not null)
        {
            var parentOk = await _db.WbsNodes.AnyAsync(
                w => w.Id == request.ParentWbsId && w.ProjectId == request.ProjectId,
                cancellationToken);
            if (!parentOk)
                throw new NotFoundException("Üst WBS bulunamadı.");
        }

        var parentMap = await _db.WbsNodes
            .Where(w => w.ProjectId == request.ProjectId)
            .ToDictionaryAsync(w => w.Id, w => w.ParentWbsId, cancellationToken);

        var id = Guid.NewGuid();
        parentMap[id] = request.ParentWbsId;
        WbsNode.ValidateNoCycle(id, request.ParentWbsId, parentMap);

        var now = _clock.UtcNow;
        var node = new WbsNode
        {
            Id = id,
            OrganizationId = project.OrganizationId,
            ProjectId = request.ProjectId,
            ParentWbsId = request.ParentWbsId,
            WbsCode = request.WbsCode.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            SortOrder = request.SortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.WbsNodes.Add(node);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(node);
    }

    internal static WbsNodeDto Map(WbsNode n) => new(
        n.Id, n.ProjectId, n.ParentWbsId, n.WbsCode, n.Name, n.Description, n.SortOrder, n.Progress);
}

public sealed record UpdateWbsNodeCommand(
    Guid ProjectId,
    Guid WbsId,
    string Name,
    string? Description,
    Guid? ParentWbsId,
    int SortOrder) : IRequest<WbsNodeDto>;

public sealed class UpdateWbsNodeValidator : AbstractValidator<UpdateWbsNodeCommand>
{
    public UpdateWbsNodeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("WBS adı zorunludur.");
    }
}

public sealed class UpdateWbsNodeHandler : IRequestHandler<UpdateWbsNodeCommand, WbsNodeDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;

    public UpdateWbsNodeHandler(IApplicationDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<WbsNodeDto> Handle(UpdateWbsNodeCommand request, CancellationToken cancellationToken)
    {
        var node = await _db.WbsNodes.FirstOrDefaultAsync(
            w => w.Id == request.WbsId && w.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("WBS bulunamadı.");

        var parentMap = await _db.WbsNodes
            .Where(w => w.ProjectId == request.ProjectId)
            .ToDictionaryAsync(w => w.Id, w => w.ParentWbsId, cancellationToken);

        parentMap[node.Id] = request.ParentWbsId;
        node.SetParent(request.ParentWbsId, parentMap);

        node.Name = request.Name.Trim();
        node.Description = request.Description;
        node.SortOrder = request.SortOrder;
        node.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return CreateWbsNodeHandler.Map(node);
    }
}

public sealed record ListWbsQuery(Guid ProjectId) : IRequest<IReadOnlyList<WbsNodeDto>>;

public sealed class ListWbsHandler : IRequestHandler<ListWbsQuery, IReadOnlyList<WbsNodeDto>>
{
    private readonly IApplicationDbContext _db;

    public ListWbsHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<WbsNodeDto>> Handle(ListWbsQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.WbsNodes.AsNoTracking()
            .Where(w => w.ProjectId == request.ProjectId)
            .OrderBy(w => w.SortOrder).ThenBy(w => w.WbsCode)
            .ToListAsync(cancellationToken);
        return list.Select(CreateWbsNodeHandler.Map).ToList();
    }
}
