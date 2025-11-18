namespace Application.Persons.GetPersonById;

/// <summary>
/// Query para obtener una persona por su identificador.
/// </summary>
/// <param name="Id">Identificador de la persona.</param>
public sealed record GetPersonByIdQuery(Guid Id) : IQuery<Result<PersonResponse>>;
