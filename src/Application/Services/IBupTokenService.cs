namespace Application.Services;

public interface IBupTokenService
{
    Task<string> GetTokenAsync(BupServiceType serviceType, CancellationToken cancellationToken);
}
