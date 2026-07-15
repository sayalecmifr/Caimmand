using Caimmand.Application.Cases.Create;
using Caimmand.Application.Cases.GetDetail;
using Caimmand.Application.Cases.List;
using Caimmand.Application.Cases.UpdateStatus;
using Caimmand.Application.Dashboard.GetDashboardKpis;
using Caimmand.Application.Timeline.AddEvent;
using Caimmand.Application.Timeline.GetTimeline;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Domain.Enums;
using Caimmand.Infrastructure;
using Caimmand.Web.Components;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Arrancando Caimmand PoC");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services) => new LoggerConfiguration()
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .CreateLogger());

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddCaimmandPersistence(builder.Configuration);

    builder.Services.AddValidatorsFromAssemblyContaining<Caimmand.Application.Marker>();
    builder.Services.AddScoped<CreateCaseHandler>();
    builder.Services.AddScoped<ListCasesHandler>();
    builder.Services.AddScoped<GetCaseDetailHandler>();
    builder.Services.AddScoped<GetDashboardKpisHandler>();
    builder.Services.AddScoped<UpdateCaseStatusHandler>();
    builder.Services.AddScoped<AddTimelineEventHandler>();
    builder.Services.AddScoped<GetTimelineHandler>();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    app.UseAntiforgery();

    app.UseSerilogRequestLogging();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    await SeedCaseDefinitionsAsync(app.Services);

    app.MapPost("/api/cases", async (CreateCaseCommand command, CreateCaseHandler handler, CancellationToken ct) =>
    {
        try
        {
            var response = await handler.Handle(command, ct);
            return Results.Created($"/api/cases/{response.Id}", response);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
            return Results.ValidationProblem(errors);
        }
    })
    .WithName("CreateCase");

    app.MapGet("/api/cases", async (string? status, string? caseDefinitionCode, ListCasesHandler handler, CancellationToken ct) =>
    {
        CaseStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CaseStatus>(status, ignoreCase: true, out var s))
        {
            parsedStatus = s;
        }
        var result = await handler.Handle(new ListCasesQuery(parsedStatus, caseDefinitionCode), ct);
        return Results.Ok(result);
    })
    .WithName("ListCases");

    app.MapGet("/api/cases/{id:guid}", async (Guid id, GetCaseDetailHandler handler, CancellationToken ct) =>
    {
        var detail = await handler.Handle(new GetCaseDetailQuery(id), ct);
        return detail is null ? Results.NotFound() : Results.Ok(detail);
    })
    .WithName("GetCase");

    app.MapPatch("/api/cases/{id:guid}/status", async (Guid id, UpdateCaseStatusCommand body, UpdateCaseStatusHandler handler, CancellationToken ct) =>
    {
        try
        {
            var response = await handler.Handle(new UpdateCaseStatusCommand(id, body.NewStatus), ct);
            return Results.Ok(response);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
            return Results.ValidationProblem(errors);
        }
    })
    .WithName("UpdateCaseStatus");

    app.MapPost("/api/cases/{id:guid}/timeline", async (Guid id, AddTimelineEventCommand body, AddTimelineEventHandler handler, CancellationToken ct) =>
    {
        try
        {
            var response = await handler.Handle(new AddTimelineEventCommand(id, body.Type, body.Origin, body.Content), ct);
            return Results.Created($"/api/cases/{id}/timeline", response);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
            return Results.ValidationProblem(errors);
        }
    })
    .WithName("AddTimelineEvent");

    app.MapGet("/api/cases/{id:guid}/timeline", async (Guid id, GetTimelineHandler handler, CancellationToken ct) =>
    {
        var events = await handler.Handle(new GetTimelineQuery(id), ct);
        return Results.Ok(events);
    })
    .WithName("GetTimeline");

    app.Run();

    static async Task SeedCaseDefinitionsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICaimmandDbContext>();

        if (await db.CaseDefinitions.AnyAsync())
        {
            return;
        }

        db.CaseDefinitions.Add(new CaseDefinition
        {
            Code = "APPOINTMENT_REMINDER",
            Name = "Recordatorio de Turno",
            Description = "Recordatorio automatico de turnos medicos",
            Category = "Appointments",
            IsActive = true,
            DefaultPriority = "Normal",
            DisplayColor = "Blue",
            DisplayIcon = "calendar"
        });

        await db.SaveChangesAsync();
        Log.Information("Sembrando CaseDefinition APPOINTMENT_REMINDER");
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Caimmand PoC termino por una excepcion no controlada");
}
finally
{
    Log.CloseAndFlush();
}