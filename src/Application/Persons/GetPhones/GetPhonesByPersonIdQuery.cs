using Application.Persons.Dtos;

namespace Application.Persons.GetPhones;

/// <summary>
/// Query para obtener los teléfonos de una persona.
/// </summary>
/// <param name="PersonId">Identificador de la persona.</param>
public sealed record GetPhonesByPersonIdQuery(int PersonId) : IQuery<IEnumerable<BupPhoneDto>>;
