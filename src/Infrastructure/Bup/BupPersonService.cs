using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Persons.Dtos;
using Infrastructure.Bup.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Application.Services;

namespace Infrastructure.Bup;

public class BupPersonService : Application.Services.IBupPersonService
{
    private readonly IHttpClientFactory _factory;
    private readonly Application.Services.IBupTokenService _tokenService;
    private readonly BupApiOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<BupPersonService> _logger;

    public BupPersonService(IHttpClientFactory factory, Application.Services.IBupTokenService tokenService, IOptions<BupApiOptions> options, ILogger<BupPersonService> logger)
    {
        _factory = factory;
        _tokenService = tokenService;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        _logger = logger;
    }

    public async Task<BupPersonDto> GetPersonByIdAsync(int personId, string? accessToken, CancellationToken cancellationToken)
    {
        if (!_options.HasConfiguredClientId())
        {
            _logger?.LogInformation("Returning mock person for id {PersonId} because BUP is not configured.", personId);
            return BuildMockPerson(personId);
        }

        try
        {
            if (!_options.HasConfiguredCredentials())
            {
                if (_options.UseMocksWhenUnconfigured)
                {
                    _logger?.LogInformation("Returning mock person for id {PersonId} because BUP is not configured.", personId);
                    return BuildMockPerson(personId);
                }

                throw new InvalidOperationException("BUP no está configurado (ClientId/ClientSecret). Configure las credenciales para realizar llamadas reales.");
            }

            var token = string.IsNullOrWhiteSpace(accessToken)
                ? await _tokenService.GetTokenAsync(cancellationToken)
                : accessToken;
            var client = _factory.CreateClient("BupApi");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"people/{personId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-IBM-Client-Id", _options.ClientId);
            request.Headers.Add("UserName", _options.Username);

            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var root = await response.Content.ReadFromJsonAsync<BupPersonRootRaw>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Respuesta BUP inválida");

            var ok = root.messages?.Any(m => string.Equals(m.code, "GSS-200-002", StringComparison.OrdinalIgnoreCase)) ?? false;
            if (!ok)
                throw new InvalidOperationException($"BUP person service returned errors: {string.Join(',', root.messages?.Select(m => m.code ?? m.message) ?? Array.Empty<string>())}");

            var d = root.data;
            if (d == null)
                return null;

            var dto = new BupPersonDto
            {
                BupId = d.bupId,
                FirstName = d.firstName,
                LastName = d.lastName,
                RegisteredName = d.registeredName,
                BirthDate = ParseDate(d.birthDate),
                Gender = d.gender,
                PersonType = d.personType,
                IdentificationNumber = d.document?.identificationNumber,
                IdentificationTypeCode = d.document?.identificationTypeCode,
                IdentificationIssuerCountry = d.document?.identificationIssuerCountry,
                TaxIdentificationNumber = d.tributaryCode?.taxIdentificationNumber
            };

            return dto;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error obteniendo persona BUP {PersonId}", personId);

            throw; // rethrow if not in dev fallback
        }
    }

    private static BupPersonDto BuildMockPerson(int personId) => personId switch
    {
        19231437 => new BupPersonDto
        {
            BupId = personId,
            FirstName = "Gabriel",
            LastName = "González",
            RegisteredName = "Gabriel González",
            BirthDate = new DateTime(1985, 4, 12),
            Gender = 1,
            PersonType = 1,
            IdentificationNumber = "19231437",
            IdentificationTypeCode = "DNI",
            IdentificationIssuerCountry = "AR",
            TaxIdentificationNumber = "20-12345678-9"
        },
        244885 => new BupPersonDto
        {
            BupId = personId,
            FirstName = "María",
            LastName = "Pérez",
            RegisteredName = "María Pérez",
            BirthDate = new DateTime(1990, 7, 3),
            Gender = 2,
            PersonType = 1,
            IdentificationNumber = "244885",
            IdentificationTypeCode = "DNI",
            IdentificationIssuerCountry = "AR",
            TaxIdentificationNumber = "27-87654321-0"
        },
        _ => new BupPersonDto
        {
            BupId = personId,
            FirstName = "Dev",
            LastName = "User",
            RegisteredName = "Dev User",
            BirthDate = null,
            Gender = null,
            PersonType = 1,
            IdentificationNumber = personId.ToString(),
            IdentificationTypeCode = "DNI",
            IdentificationIssuerCountry = "AR",
            TaxIdentificationNumber = null
        }
    };

    private static DateTime? ParseDate(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        if (DateTime.TryParse(s, out var dt))
            return dt;
        return null;
    }
}
