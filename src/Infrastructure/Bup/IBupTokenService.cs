namespace Infrastructure.Bup;

public interface IBupTokenService
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}
