using System;

namespace Infrastructure.Bup;

public sealed class BupApiOptions
{
    private static bool IsPlaceholder(string value)
        => string.IsNullOrWhiteSpace(value)
            || value.Contains("<", StringComparison.Ordinal)
            || value.Contains(">", StringComparison.Ordinal);

    public string Catalog { get; set; } = "dev";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool UseMocksWhenUnconfigured { get; set; } = false;

    public bool HasConfiguredCredentials()
        => !IsPlaceholder(ClientId) && !IsPlaceholder(ClientSecret);

    public bool HasConfiguredClientId()
        => !IsPlaceholder(ClientId);

    public bool HasConfiguredCredentials()
        => !IsPlaceholder(ClientId) && !IsPlaceholder(ClientSecret);

    public bool HasConfiguredClientId()
        => !IsPlaceholder(ClientId);

    public string BaseUrl => $"https://external-{Catalog}-api.gruposancorseguros.com/apigss/{Catalog}";
}
