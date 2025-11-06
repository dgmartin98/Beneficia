using Gss.MinimalApis.ConfigurationSources;

namespace Api.Configuration;

public static class ConfigFilesInstaller
{
    public static void AddConfigFiles(this WebApplicationBuilder builder)
    {
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddJsonFile("logSettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"logSettings.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Add(new EnvPlaceholderConfigurationSource()); // Reemplaza las variables de entorno con el formato ${VARIABLE}
    }
}
