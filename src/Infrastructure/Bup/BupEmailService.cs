using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using Application.Persons.Dtos;
using Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Bup;

public class BupEmailService : Application.Services.IBupEmailService
{
    private readonly IHttpClientFactory _factory;
    private readonly Application.Services.IBupTokenService _tokenService;
    private readonly BupApiOptions _options;
    private readonly ILogger<BupEmailService> _logger;
    private readonly string _httpClientName;

    public BupEmailService(
        IHttpClientFactory factory,
        Application.Services.IBupTokenService tokenService,
        IOptions<BupApiOptions> options,
        ILogger<BupEmailService> logger,
        string httpClientName)
    {
        _factory = factory;
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;
        _httpClientName = httpClientName;
    }

    public async Task<IEnumerable<BupEmailDto>> GetEmailsByPersonIdAsync(int personId, string? accessToken, CancellationToken cancellationToken)
    {
        if (!_options.HasConfiguredClientId(BupServiceType.Emails))
        {
            _logger?.LogInformation("Returning empty emails for id {PersonId} because BUP is not configured.", personId);
            return Enumerable.Empty<BupEmailDto>();
        }

        try
        {
            if (!_options.HasConfiguredCredentials(BupServiceType.Emails))
            {
                if (_options.UseMocksWhenUnconfigured)
                {
                    _logger?.LogInformation("Returning empty emails for id {PersonId} because BUP is not configured.", personId);
                    return Enumerable.Empty<BupEmailDto>();
                }

                throw new InvalidOperationException("BUP no está configurado (ClientId/ClientSecret). Configure las credenciales para obtener emails reales.");
            }

            var token = string.IsNullOrWhiteSpace(accessToken)
                ? await _tokenService.GetTokenAsync(BupServiceType.Emails, cancellationToken)
                : accessToken;
            var client = _factory.CreateClient(_httpClientName);
            _logger?.LogInformation("Llamando BUP emails para persona {PersonId} (catalogo {Catalog}, usuario {Username})", personId, _options.Catalog, _options.GetUsername(BupServiceType.Emails));
            using var request = new HttpRequestMessage(HttpMethod.Get, $"people/{personId}/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-IBM-Client-Id", _options.GetClientId(BupServiceType.Emails));
            request.Headers.Add("UserName", _options.GetUsername(BupServiceType.Emails));

            var response = await client.SendAsync(request, cancellationToken);
            _logger?.LogInformation("Respuesta BUP emails {PersonId}: {StatusCode}", personId, response.StatusCode);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (BupJsonUtils.TryGetProperty(root, out var messagesElement, "messages") && messagesElement.ValueKind == JsonValueKind.Array)
            {
                var messages = messagesElement.EnumerateArray().ToList();
                var ok = messages.Any(m => string.Equals(BupJsonUtils.GetString(m, "code"), "GSS-200-002", StringComparison.OrdinalIgnoreCase));
                if (!ok)
                {
                    throw new InvalidOperationException($"BUP emails service returned errors: {string.Join(',', messages.Select(m => BupJsonUtils.GetString(m, "code") ?? BupJsonUtils.GetString(m, "message"))).Trim(',')}");
                }

                _logger?.LogInformation("Mensajes BUP emails {PersonId}: {Messages}", personId, string.Join(';', messages.Select(m => BupJsonUtils.GetString(m, "code") ?? BupJsonUtils.GetString(m, "message")).Where(m => !string.IsNullOrWhiteSpace(m))));
            }

            if (!BupJsonUtils.TryGetProperty(root, out var dataElement, "data"))
            {
                throw new InvalidOperationException("Respuesta BUP emails inválida: no se encontró la sección data.");
            }

            if (!BupJsonUtils.TryGetProperty(dataElement, out var emailsElement, "emails") || emailsElement.ValueKind != JsonValueKind.Array)
            {
                return Enumerable.Empty<BupEmailDto>();
            }

            var emails = emailsElement
                .EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Object)
                .Select(e => new BupEmailDto
                {
                    EmailId = BupJsonUtils.GetInt(e, "emailId"),
                    Email = BupJsonUtils.GetString(e, "email"),
                    EmailUseType = BupJsonUtils.GetInt(e, "emailUseType")
                })
                .ToList();

            return OrderByUseType(emails);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error obteniendo emails BUP {PersonId}", personId);

            throw;
        }
    }

    private static IEnumerable<BupEmailDto> OrderByUseType(IEnumerable<BupEmailDto> emails)
    {
        var priorities = new[] { 1, 2, 6 };
        return emails
            .OrderBy(email =>
            {
                var index = Array.IndexOf(priorities, email.EmailUseType ?? -1);
                return index >= 0 ? index : priorities.Length;
            })
            .ThenBy(email => email.EmailId ?? int.MaxValue)
            .ToList();
    }
}
