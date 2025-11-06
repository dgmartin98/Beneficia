using Swashbuckle.AspNetCore.SwaggerUI;

namespace Api.Configuration;

/// <summary>
/// Extensiones para registrar y mapear endpoints en Minimal APIs
/// </summary>
public static class SwaggerConfiguration
{
    public static WebApplication UseApiSwagger(this WebApplication app)
    {
        app.UseStaticFiles();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            var slugEnvironment = app.Environment.GetSlug();
            // Personalización adicional
            c.DocumentTitle = $"[{slugEnvironment}] Cross.ServiciosCross.BeneficiaApi API Docs";
            c.SwaggerEndpoint("/swagger/v1/swagger.json", $"v1");
            c.RoutePrefix = "swagger";
            c.DocExpansion(DocExpansion.List);
            c.DefaultModelsExpandDepth(-1); // Ocultar modelos por defecto
            c.DefaultModelRendering(ModelRendering.Example);
            if (!app.Environment.IsProduction())
            {
                c.EnableTryItOutByDefault();
            }
            else
            {
                c.SupportedSubmitMethods(SubmitMethod.Get);
            }

            c.ApplyStyles(slugEnvironment.ToLower());

        });
        return app;
    }
    private static void ApplyStyles(this SwaggerUIOptions c, string environment)
    {
        c.InjectStylesheet($"/swagger-ui/custom.css");
        c.InjectStylesheet($"/swagger-ui/custom-{environment}.css");

    }
}
