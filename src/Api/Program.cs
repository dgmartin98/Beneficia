using System.Reflection;
using System.Diagnostics;
using System.Linq;
using Gss.MinimalApis.Configuration;
using Gss.MinimalApis.Middlewares;
using Gss.MinimalApis.Mediator.Configuration;
using Api.Configuration;
using Api.Persons;
using System.Text.Json;
using Gss.MinimalApis.Settings;
using Infrastructure.Persistence;
using Infrastructure.Bup;

// Configurar logging temprano para capturar errores de startup
SerilogConfiguration.ConfigureEarlyLogging();

try
{
    Log.Information("Iniciando aplicacion...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddHttpContextAccessor();

    builder.AddConfigFiles();
    builder.ConfigureSerilog();

    var swaggerSettings = builder.Configuration
        .GetSection(SwaggerUiSettings.SectionName)
        .Get<SwaggerUiSettings>() ?? new SwaggerUiSettings();

    builder.Services
        .AddEndpointsApiExplorer()
        .AddStandardApiServices(options =>
        {
            options.JsonNamingPolicy = JsonNamingPolicy.CamelCase;
            options.EnablePrettyJson = builder.Environment.IsDevelopment(); // Habilitar Pretty JSON para ambiente de desarrollo
        })
        .ConfigureApiSwagger(opt =>
        {
            opt.Title = "Cross.ServiciosCross.BeneficiaApi API Docs";
            opt.Description = "api de beneficia";
            opt.Version = "v1";
            opt.BuildDate = Assembly.GetExecutingAssembly().GetBuildDate();
            opt.XmlDocumentationFiles =
            [
                "Application.xml",
                "Infrastructure.xml",
                "Domain.xml"
            ];
        })
        // Usar Pipelines de logging y validation (FluentValidation) y el uso de CQRS (Gss.Mediator)
        .AddGssMediator([Assembly.Load("Application")], options =>
        {
            options.EnableValidationPipeline = true;
            options.EnableLoggingPipeline = true;
        })
        .ConfigureResponseCompression()
        .ConfigureRepositories(builder.Configuration)
        ;

    builder.Services.AddBupServices(builder.Configuration);

    builder.Services.AddPersonsModule();

    var app = builder.Build();

    app.UseCorrelationId(); // Agregar middleware de Correlation ID por defecto: "X-Correlation-ID"
    app.UseSerilog(); // Registrar solicitudes HTTP
    app.UseResponseCompression(); // Habilita la compresión de respuesta (Gzip/Brotli)

    app.UseStandardApiMiddleware(app.Environment.IsProduction()
        ? StandardApiOptions.Production
        : StandardApiOptions.Development); // Configurar middleware estándar de API

    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.MapEndpointsFromAssembly(Assembly.Load("Api"));
    app.MapEndpointsFromAssembly(Assembly.Load("Application"));

    // Registrar Swagger después de mapear todos los endpoints para que sean incluidos en la UI
    app.UseApiSwagger(swaggerSettings);

    // Open the browser automatically using the configured Swagger settings
    if (swaggerSettings.AutoOpenBrowser)
    {
        var url = swaggerSettings.BuildLaunchUrl(app.Urls.FirstOrDefault() ?? "https://localhost:7057");
        try
        {
            var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true };
            // start after a short delay so Kestrel is ready to accept connections
            _ = Task.Run(async () => { await Task.Delay(500); Process.Start(psi); });
        }
        catch
        {
            // best-effort open; ignore failures in dev
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Error fatal durante el inicio de la aplicacion");
}
finally
{
    await Log.CloseAndFlushAsync();
}