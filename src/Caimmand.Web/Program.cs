using Caimmand.Application.CaseDefinitions.Create;
using Caimmand.Application.CaseDefinitions.List;
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
using Caimmand.Web.Auth;
using Caimmand.Web.Components;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
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
    builder.Services.AddCascadingAuthenticationState();

    var authOptions = builder.Configuration
        .GetSection(AuthOptions.SectionName)
        .Get<AuthOptions>() ?? new AuthOptions();
    builder.Services.AddSingleton(authOptions);
    builder.Services.AddSingleton<LoginService>();

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = authOptions.CookieName;
            options.LoginPath = "/login";
            options.LogoutPath = "/account/logout";
            options.AccessDeniedPath = "/login";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        })
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(ApiKeyAuthHandler.SchemeName, _ => { });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ApiAuth", policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(
                CookieAuthenticationDefaults.AuthenticationScheme,
                ApiKeyAuthHandler.SchemeName));
    });

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddValidatorsFromAssemblyContaining<Caimmand.Application.Marker>();
    builder.Services.AddScoped<CreateCaseDefinitionHandler>();
    builder.Services.AddScoped<ListCaseDefinitionsHandler>();
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
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.UseSerilogRequestLogging();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    await ApplyMigrationsAsync(app.Services);
    await SeedCaseDefinitionsAsync(app.Services);

    app.MapPost("/account/login", async (HttpContext ctx, LoginService login, CancellationToken ct) =>
    {
        var form = await ctx.Request.ReadFormAsync(ct);
        var username = form["username"].ToString();
        var password = form["password"].ToString();
        var returnUrl = form["returnUrl"].ToString();

        if (login.TryValidate(username, password, out var user) && user is not null)
        {
            var principal = login.BuildPrincipal(user);
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            var safe = string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
                ? "/"
                : returnUrl;
            return Results.Redirect(safe);
        }

        var q = string.IsNullOrEmpty(returnUrl) ? "?error=1" : $"?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}";
        return Results.Redirect($"/login{q}");
    }).AllowAnonymous();

    app.MapPost("/account/logout", async (HttpContext ctx, CancellationToken ct) =>
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }).AllowAnonymous();

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
    .WithName("CreateCase")
    .RequireAuthorization("ApiAuth");

    app.MapGet("/api/cases", async (string? status, string? caseDefinitionCode, string? externalId, ListCasesHandler handler, CancellationToken ct) =>
    {
        CaseStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CaseStatus>(status, ignoreCase: true, out var s))
        {
            parsedStatus = s;
        }
        var result = await handler.Handle(new ListCasesQuery(parsedStatus, caseDefinitionCode, externalId), ct);
        return Results.Ok(result);
    })
    .WithName("ListCases")
    .RequireAuthorization("ApiAuth");

    app.MapGet("/api/cases/{id:guid}", async (Guid id, GetCaseDetailHandler handler, CancellationToken ct) =>
    {
        var detail = await handler.Handle(new GetCaseDetailQuery(id), ct);
        return detail is null ? Results.NotFound() : Results.Ok(detail);
    })
    .WithName("GetCase")
    .RequireAuthorization("ApiAuth");

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
    .WithName("UpdateCaseStatus")
    .RequireAuthorization("ApiAuth");

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
    .WithName("AddTimelineEvent")
    .RequireAuthorization("ApiAuth");

    app.MapGet("/api/cases/{id:guid}/timeline", async (Guid id, GetTimelineHandler handler, CancellationToken ct) =>
    {
        var events = await handler.Handle(new GetTimelineQuery(id), ct);
        return Results.Ok(events);
    })
    .WithName("GetTimeline")
    .RequireAuthorization("ApiAuth");

    app.MapGet("/api/case-definitions", async (ListCaseDefinitionsHandler handler, CancellationToken ct) =>
    {
        var result = await handler.Handle(new ListCaseDefinitionsQuery(), ct);
        return Results.Ok(result);
    })
    .WithName("ListCaseDefinitions")
    .RequireAuthorization("ApiAuth");

    app.MapPost("/api/case-definitions", async (CreateCaseDefinitionCommand command, CreateCaseDefinitionHandler handler, CancellationToken ct) =>
    {
        try
        {
            var response = await handler.Handle(command, ct);
            return Results.Created($"/api/case-definitions/{response.Id}", response);
        }
        catch (ValidationException ex)
        {
            var err = ex.Errors
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
            return Results.ValidationProblem(err);
        }
    })
    .WithName("CreateCaseDefinition")
    .RequireAuthorization("ApiAuth");

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
            DefaultPriority = "Media",
            DisplayColor = "#3b82f6",
            DisplayIcon = "calendar"
        });

        await db.SaveChangesAsync();
        Log.Information("Sembrando CaseDefinition APPOINTMENT_REMINDER");
    }

    static async Task ApplyMigrationsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CaimmandDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Migraciones de EF Core aplicadas");
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