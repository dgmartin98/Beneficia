using System.Reflection;

namespace Api.Endpoints;

public class HealthchecksEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Verifica si la aplicación está viva. 
        app.MapGet("api/health/ready", () => StandardResults.Success("Server ready"))
            .ExcludeFromDescription()
            .AllowAnonymous();

        // Verifica si la aplicación está viva. 
        app.MapGet("api/health/live", () => StandardResults.Success("Server live"))
            .ExcludeFromDescription()
            .AllowAnonymous();

        // Verifica si la aplicación está lista para recibir solicitudes. 
        // Verifica si la base de datos está disponible.
        app.MapGet("api/health/startup", () =>
            {
                var assembly = typeof(HealthchecksEndpoint).Assembly;
                var buildDate = File.GetLastWriteTime(assembly.Location);
                try
                {
                    return StandardResults.Success(new
                    {
                        BuildDate = buildDate
                    });

                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al obtener el estado de la aplicación: {ErrorMessage}, {StackTrace}", ex.Message, ex.StackTrace);
                    return StandardResults.BusinessError("Not Ready");
                }
            })
            .ExcludeFromDescription()
            .AllowAnonymous();
    }
}
