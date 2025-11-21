namespace Infrastructure.Bup;

public sealed class BupApiOptions
{
    public string Catalog { get; set; } = "dev";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    public string BaseUrl => $"https://external-{Catalog}-api.gruposancorseguros.com/apigss/{Catalog}";
}
