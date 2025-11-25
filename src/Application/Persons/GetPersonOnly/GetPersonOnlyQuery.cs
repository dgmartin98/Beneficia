using Application.Persons.Dtos;

namespace Application.Persons.GetPersonOnly;

/// <summary>
/// Query para obtener la información de personas sin teléfonos.
/// </summary>
/// <param name="PersonId">Identificador de la persona.</param>
public sealed record GetPersonOnlyQuery(int PersonId) : IQuery<BupPersonDto>;
