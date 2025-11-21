using Application.Persons.Dtos;

namespace Infrastructure.Bup;

public interface IBupPersonService
{
    Task<BupPersonDto?> GetPersonByIdAsync(int personId, CancellationToken cancellationToken);
}
