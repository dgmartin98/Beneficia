using Microsoft.EntityFrameworkCore;

namespace Application.Extensions;

public static class EfCoreExtensions
{
    public const int MaxPageSize = 1000;
    /// <summary>
    /// Método de extensión para obtener una página de resultados de una consulta paginada.
    /// </summary>
    public static async Task<(int Total, IEnumerable<T> Items)> GetPage<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) where T : class
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "El número de página debe ser mayor o igual a 1.");
        }
        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "El tamaño de página debe ser mayor o igual a 1.");
        }
        if (pageSize > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"El tamaño de la página debe ser menor o igual a {MaxPageSize} elementos.");
        }

        query = query.AsNoTracking();

        // Calcular el total de elementos y obtener los elementos de la página solicitada
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync(cancellationToken);

        return (total, items);
    }
}