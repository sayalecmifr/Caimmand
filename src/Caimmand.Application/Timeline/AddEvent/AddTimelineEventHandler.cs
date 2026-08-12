using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Application.Timeline.AddEvent;

public sealed class AddTimelineEventHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IValidator<AddTimelineEventCommand> _validator;
    private readonly IAuditRecorder _audit;

    public AddTimelineEventHandler(ICaimmandDbContext db, IValidator<AddTimelineEventCommand> validator, IAuditRecorder audit)
    {
        _db = db;
        _validator = validator;
        _audit = audit;
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

        if (command.OriginParticipantId is not null)
        {
            var participantExists = await _db.Participants
                .AnyAsync(p => p.Id == command.OriginParticipantId.Value, ct);
            if (!participantExists)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(command.OriginParticipantId),
                        "El participante referenciado no existe (OriginParticipantId).")
                });
            }
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
            ParticipantId = command.OriginParticipantId,
            Content = command.Content,
            OccurredAt = DateTime.UtcNow
        };

        _db.TimelineEvents.Add(entity);

        var change = JsonSerializer.Serialize(new
        {
            type = command.Type,
            origin = command.Origin,
            participantId = command.OriginParticipantId,
            sequence = entity.Sequence
        });

        await _audit.RecordAsync(
            command.CaseId,
            AuditOperation.EventAdded,
            command.Origin,
            change,
            contextRef: entity.Id.ToString(),
            ct);

        await _db.SaveChangesAsync(ct);

        return new AddTimelineEventResponse(entity.Id, entity.Sequence, entity.OccurredAt);
    }
}