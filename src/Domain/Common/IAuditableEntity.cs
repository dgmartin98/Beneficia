namespace Domain.Common;
/// <summary>
/// Representa una entidad auditable con un identificador de tipo <typeparamref name="TKey"/>.
/// Esta clase es la base para todas las entidades del dominio que requieren auditoría y proporciona propiedades
/// </summary>
/// <typeparam name="TKey"></typeparam>
public interface IAuditableEntity
{
    DateTime FechaCreacion { get; set; }
    string? CreadoPor { get; set; }
    DateTime? FechaModificacion { get; set; }
    string? ModificadoPor { get; set; }
    DateTime? FechaEliminacion { get; set; }
    string? EliminadoPor { get; set; }
}