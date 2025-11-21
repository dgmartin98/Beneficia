using Application.Persons.Dtos;

namespace Application.Services;

public interface IBupPersonService
{
    Task<BupPersonDto> GetPersonByIdAsync(int personId, CancellationToken cancellationToken);
}
