using System.Text.Json;
using Caimmand.Application.Audit;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Caimmand.Application.Participants.Register;

public sealed class RegisterParticipantHandler
{
    private readonly ICaimmandDbContext _db;
    private readonly IValidator<RegisterParticipantCommand> _validator;
    private readonly IAuditRecorder _audit;

    public RegisterParticipantHandler(
        ICaimmandDbContext db,
        IValidator<RegisterParticipantCommand> validator,
        IAuditRecorder audit)
    {
        _db = db;
        _validator = validator;
        _audit = audit;
    }

    public async Task<RegisterParticipantResponse> Handle(RegisterParticipantCommand command, CancellationToken ct)
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

        Participant? participant = null;
        if (!string.IsNullOrWhiteSpace(command.ExternalId))
        {
            participant = await _db.Participants
                .FirstOrDefaultAsync(p => p.ExternalId == command.ExternalId, ct);
        }

        var isNewParticipant = false;
        if (participant is null)
        {
            participant = new Participant
            {
                Type = command.Type,
                Reference = command.Reference,
                ExternalId = command.ExternalId
            };
            _db.Participants.Add(participant);
            isNewParticipant = true;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(participant.Reference))
            {
                participant.Reference = command.Reference;
            }
            if (participant.Type != command.Type)
            {
                participant.Type = command.Type;
            }
        }

        var alreadyLinked = await _db.CaseParticipants
            .AnyAsync(cp => cp.CaseId == command.CaseId && cp.ParticipantId == participant.Id, ct);
        if (!alreadyLinked)
        {
            _db.CaseParticipants.Add(new CaseParticipant
            {
                CaseId = command.CaseId,
                ParticipantId = participant.Id,
                Rol = command.Rol
            });
        }
        else
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.Rol),
                    "El participante ya esta vinculado al caso con ese Rol.")
            });
        }

        var change = JsonSerializer.Serialize(new
        {
            participantId = participant.Id,
            type = command.Type.ToString(),
            reference = command.Reference,
            externalId = command.ExternalId,
            rol = command.Rol,
            newParticipant = isNewParticipant
        });

        await _audit.RecordAsync(
            command.CaseId,
            AuditOperation.ParticipantRegistered,
            command.Reference,
            change,
            contextRef: command.ExternalId,
            ct);

        await _db.SaveChangesAsync(ct);

        return new RegisterParticipantResponse(participant.Id, command.CaseId, command.Rol);
    }
}