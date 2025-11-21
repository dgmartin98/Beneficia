namespace Application.Services;

public interface IBupTokenService
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}
