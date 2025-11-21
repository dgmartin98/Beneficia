using Application.Persons;
using Microsoft.Extensions.Options;
using Application.Persons.GetPersonById;

namespace Api.Persons;

/// <summary>
/// Endpoints relacionados con el recurso Persons.
/// </summary>
public class PersonsEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/persons/{personId:int}", async (
                [FromRoute] int personId,
                ISender sender,
                IOptions<Infrastructure.Bup.BupApiOptions> bupOptions,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    Log.Information("Bup ClientId (endpoint): '{ClientId}'", bupOptions.Value.ClientId);
                    var result = await sender.Send(new GetPersonByIdQuery(personId), cancellationToken);
                    return result.ToHttpResult();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al obtener la persona {PersonId}", personId);
                    return Results.Problem("Ocurrió un error al obtener la persona solicitada.");
                }
            })
            .WithName("Persons_GetById")
            .WithTags("Persons")
            .WithSummary("Obtiene una persona por su Id")
            .WithDescription("Expone un mock inicial que será reemplazado por acceso a base de datos en el futuro.")
            .Produces<Application.Persons.Dtos.BupPersonDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
