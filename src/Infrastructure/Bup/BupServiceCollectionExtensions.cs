using System;
using System.Text.Json;
using Application.Persons.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Bup;

public static class BupServiceCollectionExtensions
{
    public static IServiceCollection AddBupServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BupApiOptions>(configuration.GetSection("BupApi"));

        // Configure named HttpClient
        services.AddHttpClient("BupApi", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<BupApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<Application.Services.IBupTokenService, BupTokenService>();
        services.AddScoped<Application.Services.IBupPersonService, BupPersonService>();
        services.AddScoped<Application.Services.IBupPhoneService, BupPhoneService>();

        return services;
    }
}
