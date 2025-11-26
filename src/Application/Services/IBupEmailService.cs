using Application.Persons.Dtos;

namespace Application.Services;

public interface IBupEmailService
{
    Task<IEnumerable<BupEmailDto>> GetEmailsByPersonIdAsync(int personId, string? accessToken, CancellationToken cancellationToken);
}
