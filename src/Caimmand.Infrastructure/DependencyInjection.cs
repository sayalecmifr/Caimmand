using Caimmand.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caimmand.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCaimmandPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("La cadena de conexion 'Default' no esta configurada.");

        services.AddDbContext<CaimmandDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICaimmandDbContext>(sp => sp.GetRequiredService<CaimmandDbContext>());

        return services;
    }
}