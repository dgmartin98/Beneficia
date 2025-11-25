using System;
using Application.Services;

namespace Infrastructure.Bup;

public sealed class BupApiOptions
{
    private static bool IsPlaceholder(string value)
        => string.IsNullOrWhiteSpace(value)
            || value.Contains("<", StringComparison.Ordinal)
            || value.Contains(">", StringComparison.Ordinal);

    public string Catalog { get; set; } = "dev";

    // Default credentials
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    // Optional overrides per service
    public string? PersonClientId { get; set; }
    public string? PersonClientSecret { get; set; }
    public string? PersonUsername { get; set; }
    public string? PhonesClientId { get; set; }
    public string? PhonesClientSecret { get; set; }
    public string? PhonesUsername { get; set; }

    public string? PersonBaseUrl { get; set; }
    public string? PhonesBaseUrl { get; set; }

    public bool UseMocksWhenUnconfigured { get; set; } = false;

    private string DefaultExternalBaseUrl => $"https://external-{Catalog}-api.gruposancorseguros.com/apigss/{Catalog}";

    private static string ResolveValue(string? preferred, string fallback)
        => string.IsNullOrWhiteSpace(preferred) || IsPlaceholder(preferred) ? fallback : preferred;

    public string GetBaseUrl(BupServiceType serviceType) => serviceType switch
    {
        BupServiceType.Person => ResolveValue(PersonBaseUrl, DefaultExternalBaseUrl),
        BupServiceType.Phones => ResolveValue(PhonesBaseUrl, DefaultExternalBaseUrl),
        _ => DefaultExternalBaseUrl
    };

    public string GetClientId(BupServiceType serviceType) => serviceType switch
    {
        BupServiceType.Person => ResolveValue(PersonClientId, ClientId),
        BupServiceType.Phones => ResolveValue(PhonesClientId, ClientId),
        _ => ClientId
    };

    public string GetClientSecret(BupServiceType serviceType) => serviceType switch
    {
        BupServiceType.Person => ResolveValue(PersonClientSecret, ClientSecret),
        BupServiceType.Phones => ResolveValue(PhonesClientSecret, ClientSecret),
        _ => ClientSecret
    };

    public string GetUsername(BupServiceType serviceType) => serviceType switch
    {
        BupServiceType.Person => ResolveValue(PersonUsername, Username),
        BupServiceType.Phones => ResolveValue(PhonesUsername, Username),
        _ => Username
    };

    public bool HasConfiguredCredentials(BupServiceType serviceType)
        => !IsPlaceholder(GetClientId(serviceType)) && !IsPlaceholder(GetClientSecret(serviceType));

    public bool HasConfiguredClientId(BupServiceType serviceType)
        => !IsPlaceholder(GetClientId(serviceType));
}
