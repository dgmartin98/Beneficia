using Application.Services;
using Gss.Mediator;
using Gss.Results;
using Microsoft.Extensions.Logging;

namespace Application.Persons.GetPhoneToken;

public class GetPhoneTokenQueryHandler : IQueryHandler<GetPhoneTokenQuery, string>
{
    private readonly IBupTokenService _tokenService;
    private readonly ILogger<GetPhoneTokenQueryHandler> _logger;

    public GetPhoneTokenQueryHandler(IBupTokenService tokenService, ILogger<GetPhoneTokenQueryHandler> logger)
    {
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<string>> Handle(GetPhoneTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Solicitando token de teléfonos BUP");
            var token = await _tokenService.GetTokenAsync(BupServiceType.Phones, cancellationToken);
            _logger.LogInformation("Token de teléfonos obtenido correctamente");
            return Result<string>.Success(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener token de teléfonos BUP");
            throw;
        }
    }
}
