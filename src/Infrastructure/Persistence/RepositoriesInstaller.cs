using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces;
using Gss.CorporateApps.Data.Ado.Repositories;

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
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IAdoRepository>(_ => new AdoRepository(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
