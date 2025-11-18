using Application.Persons;
using Application.Persons.GetPersonById;

namespace Api.Persons;

/// <summary>
/// Endpoints relacionados con el recurso Persons.
/// </summary>
public class PersonsEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/persons/{id:guid}", async (
                [FromRoute] Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await sender.Send(new GetPersonByIdQuery(id), cancellationToken);
                    return result.ToMinimalApiResult();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al obtener la persona {PersonId}", id);
                    return StandardResults.Problem("Ocurrió un error al obtener la persona solicitada.");
                }
            })
            .WithName("Persons_GetById")
            .WithTags("Persons")
            .WithSummary("Obtiene una persona por su Id")
            .WithDescription("Expone un mock inicial que será reemplazado por acceso a base de datos en el futuro.")
            .Produces<PersonResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
