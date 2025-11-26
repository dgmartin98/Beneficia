using Application.Persons.Dtos;
using Application.Services;
using Gss.Mediator;
using Gss.Results;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Application.Persons.GetEmails;

public class GetEmailsByPersonIdQueryHandler : IQueryHandler<GetEmailsByPersonIdQuery, IEnumerable<BupEmailDto>>
{
    private readonly IBupEmailService _emailService;
    private readonly IBupTokenService _tokenService;
    private readonly ILogger<GetEmailsByPersonIdQueryHandler> _logger;

    public GetEmailsByPersonIdQueryHandler(
        IBupEmailService emailService,
        IBupTokenService tokenService,
        ILogger<GetEmailsByPersonIdQueryHandler> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IEnumerable<BupEmailDto>>> Handle(GetEmailsByPersonIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Obteniendo emails para la persona {PersonId}", request.PersonId);
            var token = await _tokenService.GetTokenAsync(BupServiceType.Emails, cancellationToken);
            var emails = await _emailService.GetEmailsByPersonIdAsync(request.PersonId, token, cancellationToken);
            _logger.LogInformation("Se obtuvieron {EmailCount} emails para la persona {PersonId}", emails.Count(), request.PersonId);
            return Result<IEnumerable<BupEmailDto>>.Success(emails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo emails BUP para la persona {PersonId}", request.PersonId);
            throw;
        }
    }
}
