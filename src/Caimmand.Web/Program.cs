using Caimmand.Infrastructure;
using Caimmand.Web.Components;
using FluentValidation;
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

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Caimmand PoC termino por una excepcion no controlada");
}
finally
{
    Log.CloseAndFlush();
}