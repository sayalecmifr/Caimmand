using Caimmand.Domain;
using Caimmand.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace Caimmand.Application.Timeline.AddEvent;

public sealed class AddTimelineEventHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IValidator<AddTimelineEventCommand> _validator;

    public AddTimelineEventHandler(ICaimmandDbContext db, IValidator<AddTimelineEventCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<AddTimelineEventResponse> Handle(AddTimelineEventCommand command, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var caseExists = await _db.Cases.AnyAsync(c => c.Id == command.CaseId, ct);
        if (!caseExists)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.CaseId), "El caso no existe.")
            });
        }

        var maxSequence = await _db.TimelineEvents
            .Where(e => e.CaseId == command.CaseId)
            .Select(e => (long?)e.Sequence)
            .MaxAsync(ct) ?? 0;

        var entity = new TimelineEvent
        {
            CaseId = command.CaseId,
            Sequence = maxSequence + 1,
            Type = command.Type,
            Origin = command.Origin,
            Content = command.Content,
            OccurredAt = DateTime.UtcNow
        };

        _db.TimelineEvents.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new AddTimelineEventResponse(entity.Id, entity.Sequence, entity.OccurredAt);
    }
}