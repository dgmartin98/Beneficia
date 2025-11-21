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
                    // Development fallback: if BUP is not configured, return a local mock without calling external services
                    if (string.IsNullOrWhiteSpace(bupOptions.Value.ClientId))
                    {
                        var mock = personId switch
                        {
                            19231437 => new Application.Persons.Dtos.BupPersonDto
                            {
                                BupId = 19231437,
                                FirstName = "Gabriel",
                                LastName = "González",
                                RegisteredName = "Gabriel González",
                                BirthDate = new DateTime(1985, 4, 12),
                                Gender = 1,
                                PersonType = 1,
                                IdentificationNumber = "19231437",
                                IdentificationTypeCode = "DNI",
                                IdentificationIssuerCountry = "AR",
                                TaxIdentificationNumber = "20-12345678-9",
                                Phones = new List<Application.Persons.Dtos.BupPhoneDto>
                                {
                                    new Application.Persons.Dtos.BupPhoneDto { PhoneId = 1, AreaPhoneCode = "11", PhoneNumber = "12345678", CountryPhoneCode = "54", CompletePhoneNumber = "+54 9 11 1234-5678", PhoneType = 1, PhoneUseType = 1, HasWhatsapp = true }
                                }
                            },
                            244885 => new Application.Persons.Dtos.BupPersonDto
                            {
                                BupId = 244885,
                                FirstName = "María",
                                LastName = "Pérez",
                                RegisteredName = "María Pérez",
                                BirthDate = new DateTime(1990, 7, 3),
                                Gender = 2,
                                PersonType = 1,
                                IdentificationNumber = "244885",
                                IdentificationTypeCode = "DNI",
                                IdentificationIssuerCountry = "AR",
                                TaxIdentificationNumber = "27-87654321-0",
                                Phones = new List<Application.Persons.Dtos.BupPhoneDto>
                                {
                                    new Application.Persons.Dtos.BupPhoneDto { PhoneId = 2, AreaPhoneCode = "11", PhoneNumber = "87654321", CountryPhoneCode = "54", CompletePhoneNumber = "+54 11 8765-4321", PhoneType = 2, PhoneUseType = 1, HasWhatsapp = false }
                                }
                            },
                            _ => new Application.Persons.Dtos.BupPersonDto { BupId = personId, FirstName = "Dev", LastName = "User", RegisteredName = "Dev User", PersonType = 1, Phones = new() }
                        };

                        return Results.Ok(mock);
                    }

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
