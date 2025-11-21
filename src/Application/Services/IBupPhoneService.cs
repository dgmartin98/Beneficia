using Application.Persons.Dtos;

namespace Application.Services;

public interface IBupPhoneService
{
    Task<IEnumerable<BupPhoneDto>> GetPhonesByPersonIdAsync(int personId, string? accessToken, CancellationToken cancellationToken);
}
