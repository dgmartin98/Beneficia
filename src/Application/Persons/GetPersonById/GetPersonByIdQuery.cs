using Application.Persons.Dtos;

namespace Application.Persons.GetPersonById;

/// <summary>
/// Query para obtener una persona por su identificador.
/// </summary>
/// <param name="PersonId">Identificador de la persona.</param>
public sealed record GetPersonByIdQuery(int PersonId) : IQuery<BupPersonDto>;
