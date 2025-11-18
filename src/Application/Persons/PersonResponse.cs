namespace Application.Persons;

/// <summary>
/// DTO expuesto por la API para representar a una persona.
/// </summary>
public class PersonResponse
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateTime FechaNacimiento { get; init; }
}
