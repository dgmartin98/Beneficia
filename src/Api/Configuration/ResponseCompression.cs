using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace Api.Configuration;

public static class ResponseCompressionConfiguration
{
    /// <summary>
    /// Configura la compresión de respuesta con Gzip
    /// </summary>
    /// <param name="services">La colección de servicios</param>
    /// <returns>La colección de servicios</returns>
    public static IServiceCollection ConfigureResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            // Habilita la compresión para todos los proveedores configurados
            options.EnableForHttps = true;

            // Configura los MIME types que se van a comprimir
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
                "application/json",
            ]);

            // Configura los proveedores de compresión disponibles
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
        });

        // Configura las opciones específicas de Gzip
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

        // Configura las opciones específicas de Brotli
        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

        return services;
    }
}
