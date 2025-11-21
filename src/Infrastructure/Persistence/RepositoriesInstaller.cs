using Application.Interfaces;
using Gss.CorporateApps.Data.Ado.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Infrastructure.Persistence;

public static class RepositoriesConfiguration
{
    /// <summary>
    /// Configura los repositorios y la unidad de trabajo
    /// </summary>
    /// <param name="services">La colección de servicios</param>
    /// <param name="configuration"></param>
    /// <returns>La colección de servicios</returns>
    public static IServiceCollection ConfigureRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Evita que la aplicación falle en ambientes locales sin una cadena de conexión configurada
            connectionString = "Server=localhost;Database=Beneficia;Trusted_Connection=True;TrustServerCertificate=True;";
            Log.Warning("No se encontró ConnectionStrings:DefaultConnection. Se usa cadena de conexión local por defecto para permitir el arranque de la API y el acceso a Swagger.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IAdoRepository>(_ => new AdoRepository(connectionString));

        return services;
    }
}
