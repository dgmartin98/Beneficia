using Application.Persons.Dtos;
using Application.Services;
using Gss.Mediator;
using Gss.Results;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Application.Persons.GetPhones;

public class GetPhonesByPersonIdQueryHandler : IQueryHandler<GetPhonesByPersonIdQuery, IEnumerable<BupPhoneDto>>
{
    private readonly IBupPhoneService _phoneService;
    private readonly IBupTokenService _tokenService;
    private readonly ILogger<GetPhonesByPersonIdQueryHandler> _logger;

    public GetPhonesByPersonIdQueryHandler(
        IBupPhoneService phoneService,
        IBupTokenService tokenService,
        ILogger<GetPhonesByPersonIdQueryHandler> logger)
    {
        _phoneService = phoneService ?? throw new ArgumentNullException(nameof(phoneService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IEnumerable<BupPhoneDto>>> Handle(GetPhonesByPersonIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Obteniendo teléfonos para la persona {PersonId}", request.PersonId);
            var token = await _tokenService.GetTokenAsync(BupServiceType.Phones, cancellationToken);
            var phones = await _phoneService.GetPhonesByPersonIdAsync(request.PersonId, token, cancellationToken);
            _logger.LogInformation("Se obtuvieron {PhoneCount} teléfonos para la persona {PersonId}", phones.Count(), request.PersonId);
            return Result<IEnumerable<BupPhoneDto>>.Success(phones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo teléfonos BUP para la persona {PersonId}", request.PersonId);
            throw;
        }
    }
}
