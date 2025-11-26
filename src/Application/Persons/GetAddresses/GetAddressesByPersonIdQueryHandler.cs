using Application.Persons.Dtos;
using Application.Services;
using Gss.Mediator;
using Gss.Results;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Application.Persons.GetAddresses;

public class GetAddressesByPersonIdQueryHandler : IQueryHandler<GetAddressesByPersonIdQuery, IEnumerable<BupAddressDto>>
{
    private readonly IBupAddressService _addressService;
    private readonly IBupTokenService _tokenService;
    private readonly ILogger<GetAddressesByPersonIdQueryHandler> _logger;

    public GetAddressesByPersonIdQueryHandler(
        IBupAddressService addressService,
        IBupTokenService tokenService,
        ILogger<GetAddressesByPersonIdQueryHandler> logger)
    {
        _addressService = addressService ?? throw new ArgumentNullException(nameof(addressService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IEnumerable<BupAddressDto>>> Handle(GetAddressesByPersonIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Obteniendo domicilios para la persona {PersonId}", request.PersonId);
            var token = await _tokenService.GetTokenAsync(BupServiceType.Addresses, cancellationToken);
            var addresses = await _addressService.GetAddressesByPersonIdAsync(request.PersonId, token, cancellationToken);
            _logger.LogInformation("Se obtuvieron {AddressCount} domicilios para la persona {PersonId}", addresses.Count(), request.PersonId);
            return Result<IEnumerable<BupAddressDto>>.Success(addresses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo domicilios BUP para la persona {PersonId}", request.PersonId);
            throw;
        }
    }
}
