using Application.Persons.Dtos;

namespace Infrastructure.Bup;

public interface IBupEmailService
{
    Task<IEnumerable<BupEmailDto>> GetEmailsByPersonIdAsync(int personId, CancellationToken cancellationToken);
}
