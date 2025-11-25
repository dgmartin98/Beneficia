namespace Application.Persons.GetPhoneToken;

/// <summary>
/// Query para obtener un token de acceso al servicio de teléfonos.
/// </summary>
public sealed record GetPhoneTokenQuery() : IQuery<string>;
