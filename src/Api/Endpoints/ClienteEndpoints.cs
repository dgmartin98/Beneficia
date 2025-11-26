using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public class ClienteEndpoints : IEndpoint
{    
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cliente")
                       .WithTags("Cliente");

        group.MapGet("/{id:int}", (int id) =>
        {
            var response = new ClienteResponse
            {
                Id = id,
                Nombre = "Diego Martín",
                Estado = "OK",
                Documento = "30-12345678-9",
                Email = "diego.martin@example.com",
                Telefono = "+54 9 11 1234-5678"
            };

            return Results.Ok(response);
        })
        .WithSummary("Obtiene información de un cliente")
        .WithDescription("Devuelve un objeto mock con datos de cliente.")
        .WithOpenApi()
        .Produces<ClienteResponse>(StatusCodes.Status200OK);
    }
}

public class ClienteResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Estado { get; set; } = default!;
    public string Documento { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Telefono { get; set; } = default!;
}
