using Application.Persons.Dtos;
using Application.Services;
using Gss.Mediator;
using Gss.Results;
using Microsoft.Extensions.Logging;

namespace Application.Persons.GetPersonOnly;

public class GetPersonOnlyQueryHandler : IQueryHandler<GetPersonOnlyQuery, BupPersonDto>
{
    private readonly IBupPersonService _personService;
    private readonly IBupTokenService _tokenService;
    private readonly ISegmentationService _segmentationService;
    private readonly ILogger<GetPersonOnlyQueryHandler> _logger;

    public GetPersonOnlyQueryHandler(
        IBupPersonService personService,
        IBupTokenService tokenService,
        ISegmentationService segmentationService,
        ILogger<GetPersonOnlyQueryHandler> logger)
    {
        _personService = personService ?? throw new ArgumentNullException(nameof(personService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _segmentationService = segmentationService ?? throw new ArgumentNullException(nameof(segmentationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<BupPersonDto>> Handle(GetPersonOnlyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Obteniendo persona BUP {PersonId} sin teléfonos", request.PersonId);
            var token = await _tokenService.GetTokenAsync(BupServiceType.Person, cancellationToken);
            var person = await _personService.GetPersonByIdAsync(request.PersonId, token, cancellationToken);

            if (person == null)
            {
                _logger.LogWarning("Persona {PersonId} no encontrada en BUP", request.PersonId);
                return Result<BupPersonDto>.NotFound("Persona no encontrada");
            }

            person.Segmentation = await _segmentationService.GetSegmentationAsync(request.PersonId, cancellationToken);

            _logger.LogInformation("Persona {PersonId} obtenida correctamente", request.PersonId);
            return Result<BupPersonDto>.Success(person);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo persona BUP {PersonId}", request.PersonId);
            throw;
        }
    }
}
