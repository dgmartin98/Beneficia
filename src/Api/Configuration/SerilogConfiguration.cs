using Serilog.Filters;

namespace Api.Configuration;

public static class SerilogConfiguration
{

    /// <summary>
    /// Configura Serilog para registrar eventos de la aplicación.
    /// </summary>
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            const string pathProperty = "RequestPath";
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Filter.ByExcluding(Matching.WithProperty(pathProperty, "/api/health/live"))
                .Filter.ByExcluding(Matching.WithProperty(pathProperty, "/api/health/ready"))
                // Excluye todos los logs relacionados con swagger
                .Filter.ByExcluding(e => e.Properties.ContainsKey(pathProperty) &&
                                         e.Properties[pathProperty].ToString().Contains("/swagger"))
                .Enrich.WithProperty("Ambiente", context.HostingEnvironment.EnvironmentName);
        });
    }

    /// <summary>
    /// Configura Serilog para registrar eventos durante el inicio de la aplicación.    
    /// </summary>
    public static void ConfigureEarlyLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Aplicacion", "UserManagement")
            .Enrich.WithProperty("Stage", "Startup")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [STARTUP] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }


    public static WebApplication UseSerilog(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondio {StatusCode} en {Elapsed:0.0000} ms";
            options.IncludeQueryInRequestPath = true;

            options.GetLevel = (httpContext, _, ex) =>
            {
                if (ex != null)
                {
                    return LogEventLevel.Error;
                }
                if (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
                {
                    return LogEventLevel.Warning;
                }
                return LogEventLevel.Information;
            };
        });

        return app;
    }
}
