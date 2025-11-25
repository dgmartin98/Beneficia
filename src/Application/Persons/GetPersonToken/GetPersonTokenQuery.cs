namespace Application.Persons.GetPersonToken;

/// <summary>
/// Query para obtener un token de acceso al servicio de personas.
/// </summary>
public sealed record GetPersonTokenQuery() : IQuery<string>;
