using Application.Services;

namespace Infrastructure.Bup;

public interface IBupTokenService
{
    Task<string> GetTokenAsync(BupServiceType serviceType, CancellationToken cancellationToken);
}
