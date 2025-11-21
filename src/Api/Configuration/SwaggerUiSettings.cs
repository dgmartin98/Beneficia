namespace Api.Configuration;

public class SwaggerUiSettings
{
    public const string SectionName = "Swagger";

    public bool AutoOpenBrowser { get; set; } = true;

    public string? LaunchUrl { get; set; }
        = "/swagger"; // default swagger UI path

    public bool EnableTryItOut { get; set; } = true;

    public bool AllowAllSubmitMethods { get; set; } = true;

    public string RoutePrefix { get; set; } = "swagger";

    public string BuildLaunchUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(LaunchUrl))
        {
            return baseUrl;
        }

        if (LaunchUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return LaunchUrl;
        }

        return $"{baseUrl.TrimEnd('/')}/{LaunchUrl.TrimStart('/')}";
    }
}
