using Application.Persons.Dtos;
using Application.Services;
using Application.Interfaces;
using Gss.Results;
using Gss.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Application.Persons.GetPersonById;

/// <summary>
/// Handler para obtener una persona por Id utilizando CQRS/MediatR.
/// </summary>
public class GetPersonByIdQueryHandler : IQueryHandler<GetPersonByIdQuery, BupPersonDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IBupPersonService _personService;
    private readonly IBupPhoneService _phoneService;
    private readonly IBupTokenService _tokenService;
    private readonly ILogger<GetPersonByIdQueryHandler> _logger;

    public GetPersonByIdQueryHandler(IAppDbContext dbContext, IBupPersonService personService, IBupPhoneService phoneService, IBupTokenService tokenService, ILogger<GetPersonByIdQueryHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _personService = personService ?? throw new ArgumentNullException(nameof(personService));
        _phoneService = phoneService ?? throw new ArgumentNullException(nameof(phoneService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<BupPersonDto>> Handle(GetPersonByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Iniciando obtención de persona {PersonId}", request.PersonId);

            var accessToken = await _tokenService.GetTokenAsync(cancellationToken);
            _logger.LogInformation("Token obtenido para persona {PersonId}", request.PersonId);

            var person = await _personService.GetPersonByIdAsync(request.PersonId, accessToken, cancellationToken);
            if (person == null)
            {
                _logger.LogWarning("Persona {PersonId} no encontrada en BUP", request.PersonId);
                return Result<BupPersonDto>.NotFound("Persona no encontrada");
            }

            var phones = await _phoneService.GetPhonesByPersonIdAsync(request.PersonId, accessToken, cancellationToken);
            person.Phones = phones.ToList();

            _logger.LogInformation("Persona {PersonId} obtenida con {PhoneCount} teléfonos", request.PersonId, person.Phones.Count);

            return Result<BupPersonDto>.Success(person);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo persona BUP {PersonId}", request.PersonId);
            throw;
        }
    }
}
