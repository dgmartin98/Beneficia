using Application.Persons.Dtos;
using Application.Services;
using Application.Interfaces;
using Gss.Results;
using Gss.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Application.Persons.GetPersonById;

/// <summary>
/// Handler para obtener una persona por Id utilizando CQRS/MediatR.
/// </summary>
public class GetPersonByIdQueryHandler : IQueryHandler<GetPersonByIdQuery, BupPersonDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IBupPersonService _personService;
    private readonly IBupPhoneService _phoneService;
    private readonly IBupEmailService _emailService;
    private readonly IBupTokenService _tokenService;
    private readonly ILogger<GetPersonByIdQueryHandler> _logger;

    public GetPersonByIdQueryHandler(IAppDbContext dbContext, IBupPersonService personService, IBupPhoneService phoneService, IBupEmailService emailService, IBupTokenService tokenService, ILogger<GetPersonByIdQueryHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _personService = personService ?? throw new ArgumentNullException(nameof(personService));
        _phoneService = phoneService ?? throw new ArgumentNullException(nameof(phoneService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<BupPersonDto>> Handle(GetPersonByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Iniciando obtención de persona {PersonId}", request.PersonId);

            var personToken = await _tokenService.GetTokenAsync(BupServiceType.Person, cancellationToken);
            _logger.LogInformation("Token de persona obtenido para {PersonId}", request.PersonId);

            var phoneToken = await _tokenService.GetTokenAsync(BupServiceType.Phones, cancellationToken);
            _logger.LogInformation("Token de teléfonos obtenido para {PersonId}", request.PersonId);

            var emailToken = await _tokenService.GetTokenAsync(BupServiceType.Emails, cancellationToken);
            _logger.LogInformation("Token de emails obtenido para {PersonId}", request.PersonId);

            var person = await _personService.GetPersonByIdAsync(request.PersonId, personToken, cancellationToken);
            if (person == null)
            {
                _logger.LogWarning("Persona {PersonId} no encontrada en BUP", request.PersonId);
                return Result<BupPersonDto>.NotFound("Persona no encontrada");
            }

            var phones = await _phoneService.GetPhonesByPersonIdAsync(request.PersonId, phoneToken, cancellationToken);
            person.Phones = phones.ToList();

            var emails = await _emailService.GetEmailsByPersonIdAsync(request.PersonId, emailToken, cancellationToken);
            person.Emails = emails.ToList();

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
