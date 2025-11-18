using Domain.Persons;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persons;

/// <summary>
/// Implementación de <see cref="IPersonRepository"/> basada en Entity Framework Core.
/// Contiene un mock inicial y deja preparado el código real para consultas a la base de datos.
/// </summary>
public class PersonRepository(AppDbContext dbContext) : IPersonRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // MOCK INICIAL: devuelve una persona en memoria sin consultar la base de datos.
        var mockPerson = new Person
        {
            Id = id,
            Nombre = "Ada",
            Apellido = "Lovelace",
            Email = "ada.lovelace@example.com",
            Phone = "+54 9 11 5555-0000",
            FechaNacimiento = new DateTime(1815, 12, 10)
        };

        return await Task.FromResult(mockPerson);

        /*
        // IMPLEMENTACIÓN REAL: utilizar el contexto para leer desde la base de datos con AsNoTracking.
        return await _dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(person => person.Id == id, cancellationToken);
        */
    }
}
