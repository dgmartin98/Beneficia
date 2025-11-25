using Application.Services;
using Gss.Mediator;
using Gss.Results;
using Microsoft.Extensions.Logging;

namespace Application.Persons.GetPersonToken;

public class GetPersonTokenQueryHandler : IQueryHandler<GetPersonTokenQuery, string>
{
    private readonly IBupTokenService _tokenService;
    private readonly ILogger<GetPersonTokenQueryHandler> _logger;

    public GetPersonTokenQueryHandler(IBupTokenService tokenService, ILogger<GetPersonTokenQueryHandler> logger)
    {
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<string>> Handle(GetPersonTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Solicitando token de personas BUP");
            var token = await _tokenService.GetTokenAsync(BupServiceType.Person, cancellationToken);
            _logger.LogInformation("Token de personas obtenido correctamente");
            return Result<string>.Success(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener token de personas BUP");
            throw;
        }
    }
}
