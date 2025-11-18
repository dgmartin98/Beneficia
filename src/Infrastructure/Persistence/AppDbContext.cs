using Application.Interfaces;
using Domain.Persons;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Person> Persons => Set<Person>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// Inicia una transacción
    /// </summary>
    /// <returns>Transacción</returns>
    public IDbContextTransaction BeginTransaction()
    {
        return Database.BeginTransaction();
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Database.OpenConnectionAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            await Database.CloseConnectionAsync();
        }
    }

}
