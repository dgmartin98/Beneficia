using System.Reflection;
using Gss.MinimalApis.Configuration;
using Gss.MinimalApis.Middlewares;
using Gss.MinimalApis.Mediator.Configuration;
using Api.Configuration;
using Api.Persons;
using System.Text.Json;
using Gss.MinimalApis.Settings;
using Infrastructure.Persistence;

// Configurar logging temprano para capturar errores de startup
SerilogConfiguration.ConfigureEarlyLogging();

try
{
    Log.Information("Iniciando aplicacion...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddHttpContextAccessor();

    builder.AddConfigFiles();
    builder.ConfigureSerilog();

    builder.Services
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

    builder.Services.AddPersonsModule();

    var app = builder.Build();

    app.UseCorrelationId(); // Agregar middleware de Correlation ID por defecto: "X-Correlation-ID"
    app.UseSerilog(); // Registrar solicitudes HTTP
    app.UseResponseCompression(); // Habilita la compresión de respuesta (Gzip/Brotli)

    app.UseStandardApiMiddleware(app.Environment.IsProduction()
        ? StandardApiOptions.Production
        : StandardApiOptions.Development); // Configurar middleware estándar de API

    app.UseApiSwagger();


    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.MapEndpointsFromAssembly(Assembly.Load("Api"));
    app.MapEndpointsFromAssembly(Assembly.Load("Application"));

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