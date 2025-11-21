using Application.Persons.Dtos;

namespace Infrastructure.Bup;

public interface IBupPhoneService
{
    Task<IEnumerable<BupPhoneDto>> GetPhonesByPersonIdAsync(int personId, CancellationToken cancellationToken);
}
