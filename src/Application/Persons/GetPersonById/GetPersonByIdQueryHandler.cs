using Application.Interfaces;
using Domain.Persons;
using Microsoft.EntityFrameworkCore;

namespace Application.Persons.GetPersonById;

/// <summary>
/// Handler para obtener una persona por Id utilizando CQRS/MediatR.
/// </summary>
public class GetPersonByIdQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetPersonByIdQuery, Result<PersonResponse>>
{
    private readonly IAppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public Task<Result<PersonResponse>> Handle(GetPersonByIdQuery request, CancellationToken cancellationToken)
    {
        // MOCK ACTIVO: devuelve una entidad en memoria preparada para los primeros pasos del módulo Persons.
        var mockPerson = new Person
        {
            Id = request.Id,
            Nombre = "Grace",
            Apellido = "Hopper",
            Email = "grace.hopper@example.com",
            Phone = "+54 9 11 5555-1234",
            FechaNacimiento = new DateTime(1906, 12, 9)
        };

        var response = MapToResponse(mockPerson);

        return Task.FromResult(Result.Success(response));

        /*
        // IMPLEMENTACIÓN REAL: descomentar para consultar la base de datos usando IAppDbContext.
        var person = await _dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (person is null)
        {
            return Result.NotFound<PersonResponse>($"No se encontró la persona con Id {request.Id}.");
        }

        return Result.Success(MapToResponse(person));
        */
    }

    private static PersonResponse MapToResponse(Person person) => new()
    {
        Id = person.Id,
        Nombre = person.Nombre,
        Apellido = person.Apellido,
        Email = person.Email,
        Phone = person.Phone,
        FechaNacimiento = person.FechaNacimiento
    };
}
