using Caimmand.Application.Cases.Create;
using Caimmand.Domain;
using Caimmand.Domain.Entities;
using Caimmand.Infrastructure;
using Caimmand.Web.Components;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;

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

    builder.Services.AddCaimmandPersistence(builder.Configuration);

    builder.Services.AddValidatorsFromAssemblyContaining<Caimmand.Application.Marker>();
    builder.Services.AddScoped<CreateCaseHandler>();

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

    app.MapPost("/cases", async (CreateCaseCommand command, CreateCaseHandler handler, CancellationToken ct) =>
    {
        try
        {
            var response = await handler.Handle(command, ct);
            return Results.Created($"/cases/{response.Id}", response);
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