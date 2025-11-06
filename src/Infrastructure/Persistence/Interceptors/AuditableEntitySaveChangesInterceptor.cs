using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Domain.Common;

namespace Infrastructure.Persistence.Interceptors;

public class AuditableEntitySaveChangesInterceptor : ISaveChangesInterceptor
{
    // IDEA: Agregar usuario actual desde el contexto (si está disponible)
    public AuditableEntitySaveChangesInterceptor() { }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
            return ValueTask.FromResult(result);

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity entity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.FechaCreacion = DateTime.Now;
                        entity.CreadoPor ??= "System";
                        entity.FechaModificacion = DateTime.Now;
                        entity.ModificadoPor ??= "System";
                        break;
                    case EntityState.Modified:
                        entity.FechaModificacion = DateTime.Now;
                        entity.ModificadoPor ??= "System";
                        break;
                    case EntityState.Deleted:
                        entity.FechaEliminacion = DateTime.Now;
                        entity.EliminadoPor ??= "System";
                        entry.State = EntityState.Modified;
                        break;
                }
            }
        }

        return ValueTask.FromResult(result);
    }
}
