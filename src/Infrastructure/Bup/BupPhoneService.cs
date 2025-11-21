using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Persons.Dtos;
using Infrastructure.Bup.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Application.Services;

namespace Infrastructure.Bup;

public class BupPhoneService : Application.Services.IBupPhoneService
{
    private readonly IHttpClientFactory _factory;
    private readonly Application.Services.IBupTokenService _tokenService;
    private readonly BupApiOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<BupPhoneService> _logger;

    public BupPhoneService(IHttpClientFactory factory, Application.Services.IBupTokenService tokenService, IOptions<BupApiOptions> options, ILogger<BupPhoneService> logger)
    {
        _factory = factory;
        _tokenService = tokenService;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        _logger = logger;
    }

    public async Task<IEnumerable<BupPhoneDto>> GetPhonesByPersonIdAsync(int personId, string? accessToken, CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.HasConfiguredCredentials())
            {
                if (_options.UseMocksWhenUnconfigured)
                {
                    _logger?.LogInformation("Returning mock phones for id {PersonId} because BUP is not configured.", personId);
                    return BuildMockPhones(personId);
                }

                throw new InvalidOperationException("BUP no está configurado (ClientId/ClientSecret). Configure las credenciales para obtener teléfonos reales.");
            }

            var token = string.IsNullOrWhiteSpace(accessToken)
                ? await _tokenService.GetTokenAsync(cancellationToken)
                : accessToken;
            var client = _factory.CreateClient("BupApi");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"people/{personId}/phones");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-IBM-Client-Id", _options.ClientId);
            request.Headers.Add("UserName", _options.Username);

            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var root = await response.Content.ReadFromJsonAsync<BupPhonesRootRaw>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Respuesta BUP phones inválida");

            var ok = root.messages?.Any(m => string.Equals(m.code, "GSS-200-002", StringComparison.OrdinalIgnoreCase)) ?? false;
            if (!ok)
                throw new InvalidOperationException($"BUP phones service returned errors: {string.Join(',', root.messages?.Select(m => m.code ?? m.message) ?? Array.Empty<string>())}");

            var phones = root.data?.phones ?? Enumerable.Empty<BupPhoneRaw>();
            return phones.Select(p => new BupPhoneDto
            {
                PhoneId = p.phoneId,
                AreaPhoneCode = p.areaPhoneCode,
                PhoneNumber = p.phoneNumber,
                PhoneType = p.phoneType,
                PhoneUseType = p.phoneUseType,
                CountryPhoneCode = p.countryPhoneCode,
                CompletePhoneNumber = p.completePhoneNumber,
                HasWhatsapp = p.phoneCertification?.hasWhatsapp ?? false
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error obteniendo phones BUP {PersonId}", personId);
            throw;
        }
    }

    private static IEnumerable<BupPhoneDto> BuildMockPhones(int personId) => personId switch
    {
        19231437 => new List<BupPhoneDto>
        {
            new BupPhoneDto { PhoneId = 1, AreaPhoneCode = "11", PhoneNumber = "12345678", CompletePhoneNumber = "+54 9 11 1234-5678", CountryPhoneCode = "54", PhoneType = 1, PhoneUseType = 1, HasWhatsapp = true },
        },
        244885 => new List<BupPhoneDto>
        {
            new BupPhoneDto { PhoneId = 2, AreaPhoneCode = "11", PhoneNumber = "87654321", CompletePhoneNumber = "+54 11 8765-4321", CountryPhoneCode = "54", PhoneType = 2, PhoneUseType = 1, HasWhatsapp = false },
        },
        _ => new List<BupPhoneDto>()
    };
}
