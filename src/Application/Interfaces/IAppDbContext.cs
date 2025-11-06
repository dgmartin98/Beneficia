using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Interfaces;

/// <summary>
/// Interfaz que define el contrato para el contexto de base de datos de la aplicación.
/// </summary>
public interface IAppDbContext
{

    /// <summary>
    /// Guarda los cambios de forma asíncrona
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Número de entidades afectadas</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia una transacción
    /// </summary>
    /// <returns>Transacción</returns>
    IDbContextTransaction BeginTransaction();

    /// <summary>
    /// Verificar si se puede conectar a la base de datos
    /// </summary>
    /// <returns>True si se puede conectar, False en caso contrario</returns>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}