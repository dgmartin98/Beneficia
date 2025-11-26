using Application.Persons.Dtos;

namespace Application.Services;

public interface IBupAddressService
{
    Task<IEnumerable<BupAddressDto>> GetAddressesByPersonIdAsync(int personId, string? accessToken, CancellationToken cancellationToken);
}
