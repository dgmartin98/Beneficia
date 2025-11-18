using Domain.Persons;

namespace Infrastructure.Persons;

/// <summary>
/// Contrato para acceder a la información de personas.
/// </summary>
public interface IPersonRepository
{
    /// <summary>
    /// Obtiene una persona por su identificador único.
    /// </summary>
    /// <param name="id">Identificador de la persona.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Entidad <see cref="Person"/> o null si no existe.</returns>
    Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
