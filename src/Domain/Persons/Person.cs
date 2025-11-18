using Domain.Common;

namespace Domain.Persons;

/// <summary>
/// Entidad que representa a una persona dentro del dominio de Beneficia.
/// Incluye información de auditoría y soporta soft delete mediante las propiedades heredadas.
/// </summary>
public class Person : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime FechaNacimiento { get; set; }

    public DateTime FechaCreacion { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public string? EliminadoPor { get; set; }
}
