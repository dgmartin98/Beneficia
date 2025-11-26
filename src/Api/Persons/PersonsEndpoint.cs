using Application.Persons;
using Microsoft.Extensions.Options;
using Application.Persons.GetPersonById;
using Application.Persons.GetPhoneToken;
using Application.Persons.GetEmails;
using Application.Persons.GetPhones;
using Application.Persons.GetPersonOnly;
using Application.Persons.GetPersonToken;
using Application.Persons.GetAddresses;

namespace Api.Persons;

/// <summary>
/// Endpoints relacionados con el recurso Persons.
/// </summary>
public class PersonsEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/persons")
            .WithTags("Persons");

        group.MapGet("/{personId:int}", async (
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
            .WithSummary("Obtiene la información extendida de una persona")
            .WithDescription("Obtiene los datos de People, Phones y Emails de BUP para la persona solicitada.")
            .Produces<Application.Persons.Dtos.BupPersonDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{personId:int}/people", async (
                [FromRoute] int personId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetPersonOnlyQuery(personId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("Persons_GetPeople")
            .WithSummary("Obtiene únicamente la información de People para una persona")
            .Produces<Application.Persons.Dtos.BupPersonDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{personId:int}/phones", async (
                [FromRoute] int personId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetPhonesByPersonIdQuery(personId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("Persons_GetPhones")
            .WithSummary("Obtiene únicamente los teléfonos de una persona")
            .Produces<IEnumerable<Application.Persons.Dtos.BupPhoneDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{personId:int}/emails", async (
                [FromRoute] int personId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetEmailsByPersonIdQuery(personId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("Persons_GetEmails")
            .WithSummary("Obtiene únicamente los emails de una persona")
            .Produces<IEnumerable<Application.Persons.Dtos.BupEmailDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{personId:int}/addresses", async (
                [FromRoute] int personId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAddressesByPersonIdQuery(personId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("Persons_GetAddresses")
            .WithSummary("Obtiene únicamente los domicilios de una persona")
            .Produces<IEnumerable<Application.Persons.Dtos.BupAddressDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/token/phones", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetPhoneTokenQuery(), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("Persons_GetPhoneToken")
            .WithSummary("Obtiene un token para el servicio de teléfonos BUP")
            .Produces<string>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/token/people", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetPersonTokenQuery(), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("Persons_GetPeopleToken")
            .WithSummary("Obtiene un token para el servicio de personas BUP")
            .Produces<string>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
