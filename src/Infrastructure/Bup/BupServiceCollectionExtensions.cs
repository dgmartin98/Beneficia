using System;
using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Bup;

public static class BupServiceCollectionExtensions
{
    public const string PhonesClientName = "BupPhonesApi";
    public const string PeopleClientName = "BupPeopleApi";
    public const string EmailsClientName = "BupEmailsApi";
    public const string AddressesClientName = "BupAddressesApi";

    public static IServiceCollection AddBupServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BupApiOptions>(configuration.GetSection("BupApi"));

        services.AddHttpClient(PhonesClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<BupApiOptions>>().Value;
            client.BaseAddress = new Uri(options.GetBaseUrl(BupServiceType.Phones));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient(EmailsClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<BupApiOptions>>().Value;
            client.BaseAddress = new Uri(options.GetBaseUrl(BupServiceType.Emails));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient(PeopleClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<BupApiOptions>>().Value;
            client.BaseAddress = new Uri(options.GetBaseUrl(BupServiceType.Person));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient(AddressesClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<BupApiOptions>>().Value;
            client.BaseAddress = new Uri(options.GetBaseUrl(BupServiceType.Addresses));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<Application.Services.IBupTokenService>(sp =>
            new BupTokenService(sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IOptions<BupApiOptions>>(),
                sp.GetRequiredService<ILogger<BupTokenService>>(),
                PeopleClientName,
                PhonesClientName,
                EmailsClientName,
                AddressesClientName));

        services.AddScoped<Application.Services.IBupPersonService>(sp =>
            new BupPersonService(sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Application.Services.IBupTokenService>(),
                sp.GetRequiredService<IOptions<BupApiOptions>>(),
                sp.GetRequiredService<ILogger<BupPersonService>>(),
                PeopleClientName));

        services.AddScoped<Application.Services.IBupPhoneService>(sp =>
            new BupPhoneService(sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Application.Services.IBupTokenService>(),
                sp.GetRequiredService<IOptions<BupApiOptions>>(),
                sp.GetRequiredService<ILogger<BupPhoneService>>(),
                PhonesClientName));

        services.AddScoped<Application.Services.IBupEmailService>(sp =>
            new BupEmailService(sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Application.Services.IBupTokenService>(),
                sp.GetRequiredService<IOptions<BupApiOptions>>(),
                sp.GetRequiredService<ILogger<BupEmailService>>(),
                EmailsClientName));

        services.AddScoped<Application.Services.IBupAddressService>(sp =>
            new BupAddressService(sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Application.Services.IBupTokenService>(),
                sp.GetRequiredService<IOptions<BupApiOptions>>(),
                sp.GetRequiredService<ILogger<BupAddressService>>(),
                AddressesClientName));

        return services;
    }
}
