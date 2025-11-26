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
    public string? EmailsClientId { get; set; }
    public string? EmailsClientSecret { get; set; }
    public string? EmailsUsername { get; set; }

    public string? PersonBaseUrl { get; set; }
    public string? PhonesBaseUrl { get; set; }
    public string? EmailsBaseUrl { get; set; }

    public bool UseMocksWhenUnconfigured { get; set; } = false;

    private string DefaultExternalBaseUrl => $"https://external-{Catalog}-api.gruposancorseguros.com/apigss/{Catalog}";

    private static string ResolveValue(string? preferred, string fallback)
        => string.IsNullOrWhiteSpace(preferred) || IsPlaceholder(preferred) ? fallback : preferred;

    private string EnsureCatalogInUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        var catalog = Catalog.ToLowerInvariant();
        var normalized = url
            .Replace("{catalog}", catalog, StringComparison.OrdinalIgnoreCase)
            .Replace("{Catalog}", catalog, StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');

        var ensured = normalized.Contains($"/{catalog}", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"{normalized}/{catalog}";

        return ensured.EndsWith("/", StringComparison.Ordinal) ? ensured : $"{ensured}/";
    }

    public string GetBaseUrl(BupServiceType serviceType) => EnsureCatalogInUrl(serviceType switch
    {
        BupServiceType.Person => ResolveValue(PersonBaseUrl, DefaultExternalBaseUrl),
        BupServiceType.Phones => ResolveValue(PhonesBaseUrl, DefaultExternalBaseUrl),
        BupServiceType.Emails => ResolveValue(EmailsBaseUrl, DefaultExternalBaseUrl),
        _ => DefaultExternalBaseUrl
    });

    public string GetClientId(BupServiceType serviceType) => serviceType switch
    {
        BupServiceType.Person => ResolveValue(PersonClientId, ClientId),
        BupServiceType.Phones => ResolveValue(PhonesClientId, ClientId),
        BupServiceType.Emails => ResolveValue(EmailsClientId, ClientId),
        _ => ClientId
    };

    public string GetClientSecret(BupServiceType serviceType) => serviceType switch
    {
        BupServiceType.Person => ResolveValue(PersonClientSecret, ClientSecret),
        BupServiceType.Phones => ResolveValue(PhonesClientSecret, ClientSecret),
        BupServiceType.Emails => ResolveValue(EmailsClientSecret, ClientSecret),
        _ => ClientSecret
    };

    public string GetUsername(BupServiceType serviceType) => serviceType switch
    {
        BupServiceType.Person => ResolveValue(PersonUsername, Username),
        BupServiceType.Phones => ResolveValue(PhonesUsername, Username),
        BupServiceType.Emails => ResolveValue(EmailsUsername, Username),
        _ => Username
    };

    public bool HasConfiguredCredentials(BupServiceType serviceType)
        => !IsPlaceholder(GetClientId(serviceType)) && !IsPlaceholder(GetClientSecret(serviceType));

    public bool HasConfiguredClientId(BupServiceType serviceType)
        => !IsPlaceholder(GetClientId(serviceType));
}
